//用物件模擬 namespace
window.autoSnapToDecimal = function(value, decimals = 2)
{
    const rounded = Number(value.toFixed(decimals));
    const tolerance = Math.pow(10, -decimals) * 5;

    return Math.abs(value - rounded) < tolerance ? rounded : value;
};

window.multOperation = function(value, factor)
{
    const temp_val = autoSnapToDecimal(value);
    const temp_factor = autoSnapToDecimal(factor);
    return autoSnapToDecimal(temp_val * temp_factor, 3);
};

window.INV_Info_Translator = window.INV_Info_Translator || {};

window.INV_Info_Translator.Single = {
    MFR_MODEL_B0B5_str  : "",
    MFR_MODEL_B6B11_str : "",
    INV_FAULT_uint      : 0,
    INV_STATUS_uint     : 0,
    FAN_SPEED_1_uint    : 0,
    FAN_SPEED_2_uint    : 0,
    AC_VOUT_uint        : 0,
    AC_VOUT_scaling     : 0,
    OP_VA_HI_uint       : 0,
    OP_VA_HI_scaling    : 0,
    OP_VA_LO_uint       : 0,
    OP_VA_LO_scaling    : 0,
    CHG_CURR_display    : 0,

    CONST : {
        STATUS_BIT_INV      : 0x0001 << 0,
        STATUS_BIT_BYP      : 0x0001 << 1,
        STATUS_BIT_CHG      : 0x0001 << 3,
        STATUS_BIT_SAVING   : 0x0001 << 5,
    },
    
    decode : function(rcv_data)
    {
        const data = rcv_data.data;
        try
        {
            if(rcv_data.dataFormat == "Numeric")
            {
                if(data.length == 2)
                {
                    const value = (data[0] << 8) | data[1];
                    
                    return value;
                }
                // else if(data.length == 6)
                // {
                //     const value = (data[0] << 5 | data[1] << 4 | data[2] << 3 | data[3] << 2 | data[4] << 1 | data[5]);
                //     return value;
                // }
            }
            else if(rcv_data.dataFormat == "ASCII")
            {
                var temp = String.fromCharCode(...data);
                console.log("[decode][ASCII] : data = " + temp + data);
                return temp;
            }
        }
        catch
        {
            console.error("[ERROR]:", error);
        }
    },
    
    

    //這邊輸入的參數都是raw Data
    Get_INV_MFR_MODEL : function(MFR_MODEL_B0B5_str, MFR_MODEL_B6B11_str)
    {
        var temp = MFR_MODEL_B0B5_str + MFR_MODEL_B6B11_str;
        console.log("[GET_INV_MFR_MODEL] : " + temp);
        return temp;
    },

    Get_INV_Fault : function()
    {
        return this.INV_FAULT_uint & 0xFEFF;
    },

    Get_INV_Status : function()
    {

        if(this.Get_INV_Fault() != 0)
        {   //Error
            return "Error";
        }
        else if((this.INV_STATUS_uint & this.CONST.STATUS_BIT_INV != 0) && (this.INV_STATUS_uint & this.CONST.STATUS_BIT_SAVING))
        {   //Saving
            return "Saving";
        }
        else if(this.INV_STATUS_uint & this.CONST.STATUS_BIT_INV != 0)
        {   //Inverter
            return "Inverter";
        }
        else if(this.INV_STATUS_uint & this.CONST.STATUS_BIT_BYP != 0)
        {   //Bypass
            return "Bypass";
        }
        else if(this.INV_STATUS_uint & this.CONST.STATUS_BIT_CHG != 0)
        {   //Charger
            return "Charger";
        }
        else
        {   //Standby
            return "Standby";
        }
    },

    Get_INV_MFR_REV : function(rcv_data)
    {
        const data = rcv_data.data;
        let chip_versions = "";

        for(let i = 0 ; i < 6 ; i ++)
        {
            if(data[i] != 0xFF)
            {
                chip_versions = chip_versions + String(multOperation(data[i], rcv_data.scaling)) + ",";
            }
        }
        
        //delete comma
        if(chip_versions.endsWith(","))
        {
            chip_versions.slice(0, -1);
        }
        return chip_versions;
    },

    Get_INV_FAN_SPEED_1 : function(FAN_SPEED_1_uint)
    {
        return String(FAN_SPEED_1_uint) + " RPM";
    },


    Get_INV_FAN_SPEED_2 : function(FAN_SPEED_2_uint)
    {
        return String(FAN_SPEED_2_uint) + " RPM";
    },

    Get_INV_Current_Data : function()
    {   
        var Aout = 0;
        if(this.AC_VOUT_uint != 0)
        {   //Using OP_VA_HI_scaling or OP_VA_LO_scaling brings the same effect.
            var OP_VA_Combine = (multOperation((this.OP_VA_HI_uint << 16 + this.OP_VA_LO_uint), this.OP_VA_HI_scaling));
            var Aout = OP_VA_Combine / (multOperation(this.AC_VOUT_uint, this.AC_VOUT_scaling));

        }
        
        if(Aout < 0.5)
        {
            Aout = 0;
        }
        return String(Aout) + " A";
    },

    Get_INV_VA_Data : function()
    {
        var OP_VA_byte = (this.OP_VA_HI_uint << 16 + this.OP_VA_LO_uint);
        if(OP_VA_byte < 500)
        {
            OP_VA_byte = 0;
        }

        return String(multOperation(OP_VA_byte, this.OP_VA_HI_scaling)) + " W";
    },
    
    Get_INV_Current_DC_Data : function(CHG_CURR_uint, CHG_CURR_scaling)
    {
        var A_DC_out = multOperation(CHG_CURR_uint, CHG_CURR_scaling);
        
        return String(A_DC_out) + " A"; //!! json 裡面沒寫單位
    },

    Get_INV_BAT_V_Data : function(VBAT_uint, VBAT_scaling)
    {
        var Vout = multOperation(VBAT_uint, VBAT_scaling);
        return String(Vout) + " V";
    },
    
    Get_INV_Temperatures_Data : function(Temperture_uint, Temperture_scaling)
    {
        var Temp_out = multOperation(Temperture_uint, Temperture_scaling);
        return String(Temp_out) + " ℃";
    },
    
    Save_AC_VOUT : function(AC_VOUT_uint, scaling)
    {
        this.AC_VOUT_uint = AC_VOUT_uint;
        this.AC_VOUT_scaling = scaling;
    },

    Save_OP_VA_HI : function(OP_VA_HI_uint, scaling)
    {
        this.OP_VA_HI_uint = OP_VA_HI_uint;
        this.OP_VA_HI_scaling = scaling;
    },

    Save_OP_VA_LO : function(OP_VA_LO_uint, scaling)
    {
        this.OP_VA_LO_uint = OP_VA_LO_uint;
        this.OP_VA_LO_scaling = scaling;
    },
}