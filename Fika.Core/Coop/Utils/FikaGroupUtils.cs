namespace Fika.Core.Coop.Utils;

public static class FikaGroupUtils
{
    public static MatchmakerPlayerControllerClass GroupController { get; set; }
    
    public static bool InGroup => GroupController is { GroupPlayers.Count: >= 1 };
    public static int GroupSize => InGroup ? GroupController.GroupPlayers.Count : 1;
    public static bool IsGroupLeader
    {
        get
        {
            if (GroupController?.Group == null) return true;
            if (GroupController?.GroupPlayers == null) return true;
            if (GroupController.GroupPlayers.Count <= 1) return true;
            if (GroupController?.Group?.Owner?.Value?.Id == null) return true;
            return GroupController.Group.Owner.Value.Id == FikaBackendUtils.Profile.ProfileId || GroupController.IsLeader;
        }
    }
}