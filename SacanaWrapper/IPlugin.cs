namespace SacanaWrapper
{
    public interface IPluginCreator
    {
        string Name { get; }
        string Filter { get; }
        string Type { get; }

        bool InitializeWithScript { get; }
        IPlugin Create(byte[] Script);
        IPlugin Create();
    }

    public interface IPlugin
    {
        string[] Import();
        string[] Import(byte[] Script);
        byte[] Export(string[] Lines);
    }
}
