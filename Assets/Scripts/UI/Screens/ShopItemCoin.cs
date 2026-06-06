using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên mỗi gói coin trong ScreenShop. Bấm nút mua → cộng coin ngay (mock purchase,
/// chưa tích hợp Google Play Billing), lưu local + đẩy snapshot lên Firestore.
/// </summary>
public class ShopItemCoin : MonoBehaviour
{
    [SerializeField] private int coinAmount;     // số coin của gói: 200, 600, 1300...
    [SerializeField] private Button buyButton;   // nút "ButtonPrice" của gói

    private void Awake()
    {
        if (buyButton != null) buyButton.onClick.AddListener(OnClickBuy);
    }

    private void OnClickBuy()
    {
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        UserData.coin += coinAmount;
        SaveDataManager.Save();
        // Phát event để HUD coin refresh + đẩy snapshot lên Firestore (giống PopupAddHeart).
        CustomeEventSystem.Instance?.ChangeCoin(UserData.coin);
        UserDataFirebaseManager.Instance?.PushCoinSnapshot();
    }
}
