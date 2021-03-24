namespace Syadeu.Database
{
    public interface ITag
    {
        UserTagFlag UserTag { get; set; }
        CustomTagFlag CustomTag { get; set; }
    }
}
