using demoVer.Models;

namespace demoVer.Utils
{
    public readonly record struct ArrowDirections(
        ArrowDirection GridToMachine,
        ArrowDirection MachineToLoad,
        ArrowDirection MachineToBattery)
    {
        public static ArrowDirections Hidden { get; } = new(ArrowDirection.Hidden, ArrowDirection.Hidden, ArrowDirection.Hidden);

        public bool AnyVisible => GridToMachine != ArrowDirection.Hidden
                                   || MachineToLoad != ArrowDirection.Hidden
                                   || MachineToBattery != ArrowDirection.Hidden;
    }

    public static class ArrowDirectionHelper
    {
        public static ArrowDirections Resolve(ConstDefinition.SYS_Mode_Options mode, bool chargerEnabled, bool acStandby)
        {
            return mode switch
            {
                ConstDefinition.SYS_Mode_Options.INVERTER => new(ArrowDirection.Hidden, ArrowDirection.Right, ArrowDirection.Up),
                ConstDefinition.SYS_Mode_Options.SAVING => new(ArrowDirection.Hidden, ArrowDirection.Right, ArrowDirection.Up),
                ConstDefinition.SYS_Mode_Options.BY_PASS => chargerEnabled
                    ? new(ArrowDirection.Right, ArrowDirection.Right, ArrowDirection.Down)
                    : new(ArrowDirection.Right, ArrowDirection.Right, ArrowDirection.Hidden),
                ConstDefinition.SYS_Mode_Options.BY_PASS_AC_CHARGER => new(ArrowDirection.Right, ArrowDirection.Right, ArrowDirection.Down),
                ConstDefinition.SYS_Mode_Options.CHARGER => new(ArrowDirection.Right, ArrowDirection.Hidden, ArrowDirection.Down),
                ConstDefinition.SYS_Mode_Options.STANDBY => acStandby
                    ? new(ArrowDirection.Right, ArrowDirection.Hidden, ArrowDirection.Hidden)
                    : new(ArrowDirection.Hidden, ArrowDirection.Hidden, ArrowDirection.Up),
                ConstDefinition.SYS_Mode_Options.AC_OK => new(ArrowDirection.Right, ArrowDirection.Hidden, ArrowDirection.Hidden),
                ConstDefinition.SYS_Mode_Options.BATTERY_FIRST => new(ArrowDirection.Left, ArrowDirection.Right, ArrowDirection.Up),
                ConstDefinition.SYS_Mode_Options.SHUTDOWN => new(ArrowDirection.Hidden, ArrowDirection.Hidden, ArrowDirection.Up),
                ConstDefinition.SYS_Mode_Options.ERROR => ArrowDirections.Hidden,
                ConstDefinition.SYS_Mode_Options.DISCON => ArrowDirections.Hidden,
                _ => ArrowDirections.Hidden,
            };
        }

        public static string ToDisplayName(this ConstDefinition.SYS_Mode_Options mode)
        {
            return mode switch
            {
                ConstDefinition.SYS_Mode_Options.INVERTER => ConstDefinition.INVERTER_MODE_str,
                ConstDefinition.SYS_Mode_Options.SAVING => ConstDefinition.SAVING_MODE_str,
                ConstDefinition.SYS_Mode_Options.BY_PASS => ConstDefinition.BYPASS_MODE_str,
                ConstDefinition.SYS_Mode_Options.BY_PASS_AC_CHARGER => ConstDefinition.BYPASS_MODE_str,
                ConstDefinition.SYS_Mode_Options.CHARGER => ConstDefinition.CHARGING_MODE_str,
                ConstDefinition.SYS_Mode_Options.STANDBY => ConstDefinition.STANDBY_MODE_str,
                ConstDefinition.SYS_Mode_Options.AC_OK => ConstDefinition.AC_OK_str,
                ConstDefinition.SYS_Mode_Options.BATTERY_FIRST => ConstDefinition.BAT_first_MODE_str,
                ConstDefinition.SYS_Mode_Options.SHUTDOWN => ConstDefinition.SHUTDOWN_MODE_str,
                ConstDefinition.SYS_Mode_Options.ERROR => ConstDefinition.ERROR_MODE_str,
                _ => ConstDefinition.DEFAULT_MODE_str,
            };
        }
    }
}
