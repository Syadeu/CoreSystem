namespace Syadeu.Database
{
    [StaticManagerIntializeOnLoad]
    internal sealed class DataManager : StaticDataManager<DataManager>
    {
        private const string c_DataPath = "Syadeu/Data";
        
        public static string ItemDataPath => $"{c_DataPath}/Item";


        //public static string DataPath => $"{Application.dataPath}/{c_DataPath}";
    }
}
