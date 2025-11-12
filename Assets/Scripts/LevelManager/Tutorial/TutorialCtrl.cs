using System.Collections.Generic;
using UnityEngine;

public class TutorialCtrl : MonoBehaviour
{
    public Vector3 BlockNeedClick()
    {
        List<GameObject> BoardAlls = LevelManager.Instance.BoardCtrl.boardAlls;
        for (int i = BoardAlls.Count - 1; i >= 0; i--)
        {
            if (BoardAlls[i].name != "Wall" && BoardAlls[i].name != "Container")
            {
                return BoardAlls[i].transform.position;
            }
        }
        return new Vector3(-1000, -1000);
    }

    public void TutorialClick()
    {
        Vector3 pos = BlockNeedClick();
        CustomeEventSystem.Instance.TutorialPos(TutorialMode.GamePlay, pos);
    }

    public void ShowOrHideTextMatch_3(bool isShow)
    {
        CustomeEventSystem.Instance.ShowTextMatch_3(isShow);
    }

    public void ShowText(TutorialType tutorialType)
    {
        CustomeEventSystem.Instance.ChangeTextTutorial(tutorialType);
    }
}
