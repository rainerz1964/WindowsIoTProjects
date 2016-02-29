namespace rfm69Base
{
    public interface ISwitch
    {
        void config();
        byte[] getCode(bool on);
        bool On();
        bool Off();
    }
}
