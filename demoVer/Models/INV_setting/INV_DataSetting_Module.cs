using demoVer.Services;

namespace demoVer.Models
{
    public class INV_DataSetting_Module : ObservableModule
    {
        private byte _ACF;
        public byte ACF //
        {
            get => _ACF;
            set => _ACF = value;
        }

        private byte _ACV;
        public byte ACV
        {
            get => _ACV;
            set => _ACV = value;
        }

        private byte _AC_Series;
        public byte AC_Series
        {
            get => _AC_Series;
            set => _AC_Series = value;
        }
    }
}