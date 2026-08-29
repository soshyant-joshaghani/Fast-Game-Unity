using UnityEngine;

namespace FastGame
{
    /// <summary>Show one page canvas; hide the rest.</summary>
    public static class FastGameUiPages
    {
        public static void ShowOnly(params (GameObject page, bool active)[] pages)
        {
            foreach (var (page, active) in pages)
            {
                if (page != null)
                    page.SetActive(active);
            }
        }

        public static void ShowOnly(GameObject active, params GameObject[] all)
        {
            foreach (var page in all)
            {
                if (page != null)
                    page.SetActive(page == active);
            }
        }
    }
}
