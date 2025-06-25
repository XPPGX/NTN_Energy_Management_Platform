namespace demoVer.Models
{
    public class triangle_group
    {
        public int arrowLeft;
        public int arrowTop;
        public int activeIndex;
        public bool showArrows;
        public bool position_OK;

        public triangle_group()
        {
            activeIndex = 0;
            showArrows = false;
            position_OK = false;
        }

        public void set_triGroup_postion(int left, int top)
        {
            arrowLeft = left;
            arrowTop = top;
            position_OK = true;
        }

        public void show_arrows()
        {
            if (position_OK)
                showArrows = true;
        }

        public void clear_arrows() => showArrows = false;

        public bool get_showArrowFlag() => showArrows;

        public int get_arrowLeft() => arrowLeft;
        public int get_arrowTop() => arrowTop;

        public string GetTriGroupStyle() => $"left:{arrowLeft}px; top:{arrowTop}px;";
    }

}
