-- =============================================================================
--  BLOCKJAM3D — CƠ SỞ DỮ LIỆU (DATABASE SCHEMA + DỮ LIỆU TEST)
--  Đồ án tốt nghiệp — UTC.K63CNTTx-HoVaTen-MaSV-ĐATN
-- -----------------------------------------------------------------------------
--  GHI CHÚ QUAN TRỌNG
--  Game BlockJam3D vận hành trên Firebase (NoSQL): Cloud Firestore +
--  Realtime Database, kết hợp Local Storage (JSON + PlayerPrefs).
--  File .sql này là BẢN MÔ HÌNH HÓA QUAN HỆ (relational mapping) của lược đồ
--  NoSQL nói trên, phục vụ yêu cầu nộp cơ sở dữ liệu dạng .sql của đồ án.
--  Bản xuất NoSQL nguyên gốc (đúng cấu trúc Firebase thực tế) được cung cấp
--  kèm theo trong file: UTC.K63CNTTx-HoVaTen-MaSV-ĐATN-DB.json
--
--  Hệ quản trị: MySQL 8.0+ (charset utf8mb4). Có thể nạp bằng:
--      mysql -u root -p < UTC.K63CNTTx-HoVaTen-MaSV-ĐATN-DB.sql
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP DATABASE IF EXISTS blockjam3d;
CREATE DATABASE blockjam3d
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
USE blockjam3d;

-- =============================================================================
--  1. CLOUD FIRESTORE (Source of Truth)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1.1  UserData/{uid} — Hồ sơ người chơi
--      Document ID = Id người chơi (bắt đầu từ 10000000)
-- -----------------------------------------------------------------------------
CREATE TABLE UserData (
    Id                   VARCHAR(32)   NOT NULL COMMENT 'ID người chơi (document ID)',
    Name                 VARCHAR(100)  NOT NULL COMMENT 'Tên hiển thị',
    Coin                 INT           NOT NULL DEFAULT 0     COMMENT 'Số coin hiện có',
    Level                INT           NOT NULL DEFAULT 1     COMMENT 'Level hiện tại',
    Heart                INT           NOT NULL DEFAULT 5     COMMENT 'Số tim hiện có',
    Frame                INT           NOT NULL DEFAULT 0     COMMENT 'Frame avatar đang dùng',
    CreatedAt            DATETIME      NOT NULL COMMENT 'Thời điểm tạo tài khoản (server timestamp)',
    UnityPlayerId        VARCHAR(64)   NULL     COMMENT 'Unity Player Account ID (khi liên kết Google)',
    LoginType            VARCHAR(32)   NULL     COMMENT 'Loại đăng nhập, ví dụ "Google"',
    GoogleLinkedAt       DATETIME      NULL     COMMENT 'Thời điểm liên kết Google',
    Provider             VARCHAR(32)   NULL     COMMENT 'Provider name, ví dụ "unity"',
    LastLoginAt          DATETIME      NULL     COMMENT 'Thời điểm đăng nhập gần nhất',
    LastSendBoosterDate  CHAR(8)       NULL     COMMENT 'Ngày gửi booster gần nhất (yyyyMMdd, giờ VN)',
    SendBoosterCount     INT           NOT NULL DEFAULT 0     COMMENT 'Số lần gửi booster trong ngày (tối đa 3)',
    PRIMARY KEY (Id),
    INDEX idx_userdata_level (Level)
) ENGINE=InnoDB COMMENT='Hồ sơ người chơi (Firestore: UserData/{uid})';

-- -----------------------------------------------------------------------------
-- 1.1.b  UserBoosters — chuẩn hóa mảng Boosters (array<map>) trong UserData
--        Mỗi phần tử: { name, count }
-- -----------------------------------------------------------------------------
CREATE TABLE UserBoosters (
    UserId   VARCHAR(32) NOT NULL COMMENT 'FK → UserData.Id',
    Name     VARCHAR(32) NOT NULL COMMENT 'Tên booster: Undo / Add / Shuffle / Magnet',
    Count    INT         NOT NULL DEFAULT 0 COMMENT 'Số lượng booster',
    PRIMARY KEY (UserId, Name),
    CONSTRAINT fk_booster_user FOREIGN KEY (UserId)
        REFERENCES UserData (Id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Danh sách booster của người chơi (mảng Boosters)';

-- -----------------------------------------------------------------------------
-- 1.2  UserData/{uid}/Friends/{friendId} — Danh sách bạn bè (hai chiều)
-- -----------------------------------------------------------------------------
CREATE TABLE Friends (
    UserId    VARCHAR(32) NOT NULL COMMENT 'Chủ sở hữu danh sách (FK → UserData.Id)',
    FriendId  VARCHAR(32) NOT NULL COMMENT 'ID của người bạn (FK → UserData.Id)',
    CreatedAt DATETIME    NOT NULL COMMENT 'Thời điểm trở thành bạn bè',
    PRIMARY KEY (UserId, FriendId),
    CONSTRAINT fk_friend_owner  FOREIGN KEY (UserId)   REFERENCES UserData (Id) ON DELETE CASCADE,
    CONSTRAINT fk_friend_target FOREIGN KEY (FriendId) REFERENCES UserData (Id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Quan hệ bạn bè hai chiều (subcollection Friends)';

-- -----------------------------------------------------------------------------
-- 1.3  FriendRequests/{autoId} — Lời mời kết bạn đang chờ
-- -----------------------------------------------------------------------------
CREATE TABLE FriendRequests (
    Id           VARCHAR(40) NOT NULL COMMENT 'Document ID (auto-generated)',
    FromUserId   VARCHAR(32) NOT NULL COMMENT 'ID người gửi (FK → UserData.Id)',
    ToUserId     VARCHAR(32) NOT NULL COMMENT 'ID người nhận (FK → UserData.Id)',
    FromUserName VARCHAR(100) NOT NULL COMMENT 'Tên hiển thị người gửi',
    Status       VARCHAR(16) NOT NULL DEFAULT 'Pending' COMMENT 'Trạng thái: Pending',
    CreatedAt    DATETIME    NOT NULL COMMENT 'Thời điểm gửi lời mời',
    PRIMARY KEY (Id),
    INDEX idx_fr_to_status (ToUserId, Status),
    CONSTRAINT fk_fr_from FOREIGN KEY (FromUserId) REFERENCES UserData (Id) ON DELETE CASCADE,
    CONSTRAINT fk_fr_to   FOREIGN KEY (ToUserId)   REFERENCES UserData (Id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Lời mời kết bạn (Firestore: FriendRequests/{autoId})';

-- -----------------------------------------------------------------------------
-- 1.4  ServerConfigs/UserCounter — Bộ đếm ID tuần tự
-- -----------------------------------------------------------------------------
CREATE TABLE ServerConfigs (
    ConfigKey  VARCHAR(40) NOT NULL COMMENT 'Document ID, ví dụ "UserCounter"',
    LastUserID BIGINT      NOT NULL COMMENT 'ID lớn nhất đã cấp (bắt đầu từ 10000000)',
    PRIMARY KEY (ConfigKey)
) ENGINE=InnoDB COMMENT='Cấu hình server / bộ đếm ID (Firestore: ServerConfigs)';

-- =============================================================================
--  2. FIREBASE REALTIME DATABASE (Notification Bus — one-shot)
--     Các node bị xóa ngay sau khi client nhận. Ở đây lưu lại để minh họa.
-- =============================================================================

-- 2.1  friend_requests/{toUid}/{fromUid}
CREATE TABLE Notif_FriendRequests (
    ToUid     VARCHAR(32) NOT NULL COMMENT 'Người nhận',
    FromUid   VARCHAR(32) NOT NULL COMMENT 'Người gửi',
    Value     TINYINT(1)  NOT NULL DEFAULT 1 COMMENT 'Luôn = true(1)',
    PRIMARY KEY (ToUid, FromUid)
) ENGINE=InnoDB COMMENT='RTDB ping: lời mời kết bạn';

-- 2.2  friend_accept/{fromUid}/{toUid}
CREATE TABLE Notif_FriendAccept (
    FromUid   VARCHAR(32) NOT NULL,
    ToUid     VARCHAR(32) NOT NULL,
    Value     TINYINT(1)  NOT NULL DEFAULT 1,
    PRIMARY KEY (FromUid, ToUid)
) ENGINE=InnoDB COMMENT='RTDB ping: chấp nhận kết bạn';

-- 2.3  friend_decline/{fromUid}/{toUid}
CREATE TABLE Notif_FriendDecline (
    FromUid   VARCHAR(32) NOT NULL,
    ToUid     VARCHAR(32) NOT NULL,
    Value     TINYINT(1)  NOT NULL DEFAULT 1,
    PRIMARY KEY (FromUid, ToUid)
) ENGINE=InnoDB COMMENT='RTDB ping: từ chối kết bạn';

-- 2.4  send_booster/{toUid}/{pushKey}
CREATE TABLE Notif_SendBooster (
    ToUid       VARCHAR(32) NOT NULL COMMENT 'Người nhận',
    PushKey     VARCHAR(40) NOT NULL COMMENT 'Key auto-generated bởi Push()',
    FromUserId  VARCHAR(32) NOT NULL COMMENT 'Người gửi',
    BoosterName VARCHAR(32) NOT NULL COMMENT 'Undo / Shuffle / Heart ...',
    Amount      INT         NOT NULL COMMENT 'Số lượng tặng',
    PRIMARY KEY (ToUid, PushKey)
) ENGINE=InnoDB COMMENT='RTDB ping: tặng booster/heart';

-- =============================================================================
--  3. LOCAL STORAGE — JSON FILE (UserData.json) — mirror offline
-- =============================================================================

-- 3.1  UserData.json — dữ liệu người chơi offline
CREATE TABLE LocalUserData (
    DeviceRef          VARCHAR(40) NOT NULL COMMENT 'Tham chiếu thiết bị/người chơi cục bộ',
    Coin               INT         NOT NULL DEFAULT 99999 COMMENT 'Số coin (mặc định dev 99999)',
    Level              INT         NOT NULL DEFAULT 1      COMMENT 'Level hiện tại',
    Hearts             INT         NOT NULL DEFAULT 5      COMMENT 'Số tim (tối đa 5)',
    NextHeartUnixTicks BIGINT      NOT NULL DEFAULT 0      COMMENT 'Thời điểm hồi tim tiếp theo (0 = đầy)',
    Language           VARCHAR(4)  NOT NULL DEFAULT 'en'   COMMENT 'Ngôn ngữ: en / vi',
    PRIMARY KEY (DeviceRef)
) ENGINE=InnoDB COMMENT='Mirror offline của hồ sơ người chơi (UserData.json)';

-- 3.1.b  listBoosterCounters — mảng booster trong UserData.json
CREATE TABLE LocalBoosterCounters (
    DeviceRef VARCHAR(40) NOT NULL COMMENT 'FK → LocalUserData.DeviceRef',
    Name      VARCHAR(32) NOT NULL,
    Count     INT         NOT NULL DEFAULT 0,
    PRIMARY KEY (DeviceRef, Name),
    CONSTRAINT fk_localbooster FOREIGN KEY (DeviceRef)
        REFERENCES LocalUserData (DeviceRef) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Booster cục bộ (listBoosterCounters)';

-- 3.2  dailyMissionProgress — tiến trình nhiệm vụ hằng ngày
CREATE TABLE DailyMissionProgress (
    DeviceRef         VARCHAR(40) NOT NULL COMMENT 'FK → LocalUserData.DeviceRef',
    LastResetUtcTicks BIGINT      NOT NULL DEFAULT 0 COMMENT 'UTC midnight ticks (0 = chưa seed)',
    PRIMARY KEY (DeviceRef),
    CONSTRAINT fk_dmp FOREIGN KEY (DeviceRef)
        REFERENCES LocalUserData (DeviceRef) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Tiến trình nhiệm vụ hằng ngày';

-- 3.2.b  tasks[] — 3 nhiệm vụ trong ngày (MissionTaskProgress)
CREATE TABLE DailyMissionTasks (
    DeviceRef    VARCHAR(40) NOT NULL COMMENT 'FK → DailyMissionProgress.DeviceRef',
    MissionId    VARCHAR(40) NOT NULL COMMENT 'ID nhiệm vụ (tham chiếu DailyMissionData SO)',
    CurrentCount INT         NOT NULL DEFAULT 0 COMMENT 'Tiến trình hiện tại',
    Claimed      TINYINT(1)  NOT NULL DEFAULT 0 COMMENT 'Đã nhận thưởng chưa',
    PRIMARY KEY (DeviceRef, MissionId),
    CONSTRAINT fk_dmt FOREIGN KEY (DeviceRef)
        REFERENCES DailyMissionProgress (DeviceRef) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Danh sách nhiệm vụ trong ngày';

-- =============================================================================
--  4. LOCAL STORAGE — PLAYERPREFS (key-value, theo thiết bị)
-- =============================================================================
CREATE TABLE PlayerPrefs (
    DeviceRef  VARCHAR(40) NOT NULL COMMENT 'Tham chiếu thiết bị',
    PrefKey    VARCHAR(64) NOT NULL COMMENT 'PlayerID / PlayerName / Hearts / Timer ...',
    PrefValue  VARCHAR(255) NULL    COMMENT 'Giá trị (lưu dạng chuỗi)',
    PRIMARY KEY (DeviceRef, PrefKey)
) ENGINE=InnoDB COMMENT='PlayerPrefs (key-value cục bộ)';

-- =============================================================================
--  DỮ LIỆU TEST (đầy đủ, có thể chạy game ngay)
-- =============================================================================

-- ---- 1.1  UserData ---------------------------------------------------------
INSERT INTO UserData
(Id, Name, Coin, Level, Heart, Frame, CreatedAt, UnityPlayerId, LoginType, GoogleLinkedAt, Provider, LastLoginAt, LastSendBoosterDate, SendBoosterCount) VALUES
('10000000', 'Admin BlockJam',  999999, 50, 5, 3, '2026-01-01 09:00:00', 'upid_admin_0001', 'Google', '2026-01-01 09:05:00', 'unity', '2026-06-05 08:00:00', '20260605', 0),
('10000001', 'Nguyen Van A',     12500, 12, 5, 1, '2026-02-10 14:23:11', NULL,             NULL,     NULL,                  'unity', '2026-06-04 21:10:00', '20260604', 1),
('10000002', 'Tran Thi B',        3400,  7, 3, 0, '2026-02-15 10:02:45', NULL,             NULL,     NULL,                  'unity', '2026-06-03 19:45:00', NULL,        0),
('10000003', 'Le Van C',         58000, 25, 5, 2, '2026-03-01 08:30:00', 'upid_cxxx_0003', 'Google', '2026-03-02 12:00:00', 'unity', '2026-06-05 07:30:00', '20260605', 2),
('10000004', 'Pham Thi D',         150,  3, 1, 0, '2026-04-20 16:12:00', NULL,             NULL,     NULL,                  'unity', '2026-06-01 11:00:00', NULL,        0),
('10000005', 'Hoang Van E',      27800, 18, 4, 1, '2026-05-05 13:00:00', NULL,             NULL,     NULL,                  'unity', '2026-06-05 06:15:00', '20260605', 3);

-- ---- 1.1.b  UserBoosters ---------------------------------------------------
INSERT INTO UserBoosters (UserId, Name, Count) VALUES
('10000000', 'Undo', 99), ('10000000', 'Add', 99), ('10000000', 'Shuffle', 99), ('10000000', 'Magnet', 99),
('10000001', 'Undo', 2),  ('10000001', 'Add', 2),  ('10000001', 'Shuffle', 1),  ('10000001', 'Magnet', 0),
('10000002', 'Undo', 0),  ('10000002', 'Add', 1),  ('10000002', 'Shuffle', 2),  ('10000002', 'Magnet', 2),
('10000003', 'Undo', 5),  ('10000003', 'Add', 5),  ('10000003', 'Shuffle', 5),  ('10000003', 'Magnet', 5),
('10000004', 'Undo', 2),  ('10000004', 'Add', 2),  ('10000004', 'Shuffle', 2),  ('10000004', 'Magnet', 2),
('10000005', 'Undo', 3),  ('10000005', 'Add', 0),  ('10000005', 'Shuffle', 4),  ('10000005', 'Magnet', 1);

-- ---- 1.2  Friends (hai chiều) ---------------------------------------------
INSERT INTO Friends (UserId, FriendId, CreatedAt) VALUES
('10000001', '10000002', '2026-03-10 10:00:00'),
('10000002', '10000001', '2026-03-10 10:00:00'),
('10000001', '10000003', '2026-03-12 18:30:00'),
('10000003', '10000001', '2026-03-12 18:30:00'),
('10000003', '10000005', '2026-05-20 09:15:00'),
('10000005', '10000003', '2026-05-20 09:15:00');

-- ---- 1.3  FriendRequests (đang chờ) ---------------------------------------
INSERT INTO FriendRequests (Id, FromUserId, ToUserId, FromUserName, Status, CreatedAt) VALUES
('req_auto_0001', '10000004', '10000001', 'Pham Thi D',   'Pending', '2026-06-04 20:00:00'),
('req_auto_0002', '10000002', '10000005', 'Tran Thi B',   'Pending', '2026-06-05 07:45:00');

-- ---- 1.4  ServerConfigs ----------------------------------------------------
INSERT INTO ServerConfigs (ConfigKey, LastUserID) VALUES
('UserCounter', 10000005);

-- ---- 2.x  RTDB notifications (one-shot, demo) ------------------------------
INSERT INTO Notif_FriendRequests (ToUid, FromUid, Value) VALUES
('10000001', '10000004', 1),
('10000005', '10000002', 1);

INSERT INTO Notif_FriendAccept (FromUid, ToUid, Value) VALUES
('10000001', '10000003', 1);

INSERT INTO Notif_FriendDecline (FromUid, ToUid, Value) VALUES
('10000004', '10000002', 1);

INSERT INTO Notif_SendBooster (ToUid, PushKey, FromUserId, BoosterName, Amount) VALUES
('10000001', '-Npush_key_aaa1', '10000003', 'Undo', 1),
('10000001', '-Npush_key_aaa2', '10000003', 'Heart', 1),
('10000005', '-Npush_key_bbb1', '10000001', 'Shuffle', 2);

-- ---- 3.x  Local storage mirror --------------------------------------------
INSERT INTO LocalUserData (DeviceRef, Coin, Level, Hearts, NextHeartUnixTicks, Language) VALUES
('device_local_main', 99999, 12, 5, 0, 'vi');

INSERT INTO LocalBoosterCounters (DeviceRef, Name, Count) VALUES
('device_local_main', 'Undo', 2),
('device_local_main', 'Add', 2),
('device_local_main', 'Shuffle', 2),
('device_local_main', 'Magnet', 2);

INSERT INTO DailyMissionProgress (DeviceRef, LastResetUtcTicks) VALUES
('device_local_main', 638845632000000000);

INSERT INTO DailyMissionTasks (DeviceRef, MissionId, CurrentCount, Claimed) VALUES
('device_local_main', 'mission_play_3_levels', 2, 0),
('device_local_main', 'mission_use_2_boosters', 2, 1),
('device_local_main', 'mission_match_50_blocks', 31, 0);

-- ---- 4  PlayerPrefs --------------------------------------------------------
INSERT INTO PlayerPrefs (DeviceRef, PrefKey, PrefValue) VALUES
('device_local_main', 'PlayerID',   '10000001'),
('device_local_main', 'PlayerName', 'Nguyen Van A'),
('device_local_main', 'Hearts',     '5'),
('device_local_main', 'Timer',      '0'),
('device_local_main', 'LastQuitTime', '638845000000000000');

SET FOREIGN_KEY_CHECKS = 1;

-- =============================================================================
--  HẾT — Tổng: 6 người chơi, 24 booster, 3 cặp bạn bè, 2 lời mời, 6 thông báo
-- =============================================================================
