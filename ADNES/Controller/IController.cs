using ADNES.Controller.Enums;

namespace ADNES.Controller
{
    public interface IController
    {
        void ButtonPress(Buttons button);
        void ButtonRelease(Buttons button);
        void SignalController(byte input);
        byte ReadController();
    }
}