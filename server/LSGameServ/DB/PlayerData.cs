using System;

namespace LSGameServ.DB {
    [Serializable]
    public class PlayerData{
        public int score = 0;
        public int win = 0;
        public int defeat = 0;

        public PlayerData() {
            score = 100;
        }
    }
}
