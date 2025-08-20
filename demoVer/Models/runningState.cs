namespace demoVer.Models
{
    public enum ArrowDirection {Right, Left, Down, Up, Hidden}

    public class triangle_group
    {
        public int arrowLeft;
        public int arrowTop;
        public int activeIndex;
        public bool showArrows;
        public bool position_OK;

        public int Count {get; set;} = 3;
        public ArrowDirection Direction {get; private set;} = ArrowDirection.Right;

        private int _gridRow = 1;
        private int _gridCol = 1;

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
            {
                showArrows = true;
                //套用 [顯示的] css

            }
        }

        public void clear_arrows()
        {
            showArrows = false;
            //套用 [不顯示的] css

        }

        public bool get_showArrowFlag() => showArrows;

        public int get_arrowLeft() => arrowLeft;
        public int get_arrowTop() => arrowTop;

        public string GetTriGroupStyle() => $"left:{arrowLeft}px; top:{arrowTop}px;";
    
        public void PlaceAt(int gridRow, int gridCol)
        {
            _gridRow = Math.Max(1, gridRow);
            _gridCol = Math.Max(1, gridCol);

            position_OK = true;
        }

        public string GetGridAreaStyle() => $"grid-row:{_gridRow}; grid-column:{_gridCol};";

        public void SetDirection(ArrowDirection dir) => Direction = dir;
        
        public string GetTriangleBaseClass()
        {
            switch(Direction)
            {
                case ArrowDirection.Left : 
                    return "triangle left";
                case ArrowDirection.Right:
                    return "triangle";
                case ArrowDirection.Up:
                    return "triangle-downward up";
                case ArrowDirection.Down:
                    return "triangle-downward";
                case ArrowDirection.Hidden:
                    return "triangle";
                default:
                    return "triangle";
            }
        }

        public string GetGroupClass()
        {
            var dirClass = Direction is ArrowDirection.Up or ArrowDirection.Down
                ? "arrows-in-cell dir-v" : "arrows-in-cell dir-h";

            return (Direction == ArrowDirection.Hidden || !showArrows)
                ? $"{dirClass} is-invisible" : dirClass;
        }
    }

}
