namespace demoVer.Models
{
    public enum CardType
    {
        Info = 1,
        Chart = 2,
        Status = 3
    }

    public class CardInfo
    {
        public string ID {get; set;}                                        //識別用的ID(string)
        public CardType card_Type {get; set;}                                  
        public string widthClass {get; set;}                                //定義的寬度(w25 | w50)
        public byte showSequence {get; set;}                                //存被渲染的順序 start from 0
        
        // public Type ComponentType {get; set;}                               //razor的元件類型(名稱)
        // public bool IsVisible {get; set;}                                   //控制可不可視
        

        //public LayoutComponentBase ComponentRef {get; set;}                 //存Ref，之後可用這個在C#控制這些child razor元件
        //public Dictionary<string, object> Parameters {get; set;} = new();   //傳入每個card的參數
    }

    public class InfoTuple
    {
        public string IconPath { get; set; }
        public string AltText { get; set; }
        public List<(string Label, string Value)> DataPairs { get; set; }
    }
}