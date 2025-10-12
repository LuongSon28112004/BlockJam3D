using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FindingPath : MonoBehaviour
{
    public List<Container> containers;
    public int rowItem;
    public int ColItem;

    public void SetCapacity(int row, int col)
    {
        rowItem = row;
        ColItem = col;
    }

    // Hàm BFSFind trả về cả path và bool
    public async Task<(List<Vector3> path, bool hasPath)> BFSFind(Container container)
    {
        int index = containers.IndexOf(container);
        int startRow = index / ColItem;
        int startCol = index % ColItem;

        // Trường hợp đặc biệt: đang ở hàng cuối cùng
        if (startRow == rowItem - 1)
        {
            return (new List<Vector3>(), true);
        }

        var path = await FindPath(startRow, startCol);
        bool hasPath = path != null && path.Count > 0;

        return (path, hasPath);
    }

    // Hàm BFS tìm đường
    private async Task<List<Vector3>> FindPath(int startRow, int startCol)
    {
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        bool[,] visited = new bool[rowItem, ColItem];
        (int r, int c)[,] parent = new (int, int)[rowItem, ColItem];

        Queue<(int r, int c)> queue = new Queue<(int, int)>();
        queue.Enqueue((startRow, startCol));
        visited[startRow, startCol] = true;

        while (queue.Count > 0)
        {
            var (r, c) = queue.Dequeue();

            // Nếu là hàng cuối và container tồn tại
            if (r == rowItem - 1 && GetContainer(r, c) != null)
            {
                return ReconstructPath(parent, startRow, startCol, r, c);
            }

            for (int i = 0; i < 4; i++)
            {
                int nr = r + dx[i];
                int nc = c + dy[i];

                if (IsValid(nr, nc) && !visited[nr, nc])
                {
                    var neighbor = GetContainer(nr, nc);
                    if (neighbor != null && !neighbor.IsContaining)
                    {
                        visited[nr, nc] = true;
                        parent[nr, nc] = (r, c);
                        queue.Enqueue((nr, nc));
                    }
                }
            }
        }

        // Không tìm được đường đi
        return null;
    }

    private bool IsValid(int r, int c)
    {
        return r >= 0 && r < rowItem && c >= 0 && c < ColItem;
    }

    private Container GetContainer(int r, int c)
    {
        int index = r * ColItem + c;
        if (index < 0 || index >= containers.Count) return null;
        return containers[index];
    }

    private List<Vector3> ReconstructPath((int r, int c)[,] parent, int sr, int sc, int er, int ec)
    {
        List<Vector3> path = new List<Vector3>();
        int r = er, c = ec;

        while (!(r == sr && c == sc))
        {
            Container cont = GetContainer(r, c);
            if (cont != null)
                path.Add(cont.Pos);

            (r, c) = parent[r, c];
        }

        path.Add(GetContainer(sr, sc).Pos);
        path.Reverse();
        return path;
    }
}
