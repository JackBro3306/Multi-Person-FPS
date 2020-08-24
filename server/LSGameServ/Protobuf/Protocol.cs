namespace LSGameServ.Protobuf {
    public enum Protocol {
        #region tcp
        HurtBeat,
        Regist,
        Login,
        Logout,

        GetAchieve,

        CreateRoom,
        EnterRoom,
        GetRoomList,
        GetRoomInfo,
        LeaveRoom,

        ReadyFight,
        StarFight,
        Fight,

        UpdateUnitInfo,
        Shooting,
        Reload,
        Hit,
        UpdateScore,
        RelifePlayer,
        GameResult,
        FightComplete,
        #endregion
        #region udp
        CmdReady,

        #endregion
    };
}
