namespace demoVer.Models
{
    public enum ArrowDirection {Right, Left, Down, Up, Hidden}

    public class triangle_group
    {
    private const int DefaultCount = 3;

    public int arrowLeft;
        public int arrowTop;
        public int activeIndex;
        public bool showArrows;
        public bool position_OK;

    public int Count { get; private set; } = DefaultCount;
        private ArrowDirection _direction = ArrowDirection.Hidden;
        private ArrowDirection _placeholderDirection = ArrowDirection.Hidden;
        public bool UsePlaceholder { get; private set; }

        public ArrowDirection Direction => UsePlaceholder ? _placeholderDirection : _direction;
        public bool ShouldAnimate => showArrows && !UsePlaceholder && _direction != ArrowDirection.Hidden;

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

        public void SetDirection(ArrowDirection dir)
        {
            _direction = dir;
            if (!UsePlaceholder && activeIndex < 0)
            {
                activeIndex = 0;
            }
        }

        public void SetPlaceholder(bool enabled, ArrowDirection fallbackDirection)
        {
            UsePlaceholder = enabled;
            if (enabled)
            {
                _placeholderDirection = fallbackDirection;
                activeIndex = -1;
                Count = 1;
            }
            else
            {
                _placeholderDirection = ArrowDirection.Hidden;
                if (activeIndex < 0)
                {
                    activeIndex = 0;
                }
                Count = DefaultCount;
            }
        }
        
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

        public string GetTriangleClass(int index)
        {
            if (UsePlaceholder)
            {
                var orientation = Direction is ArrowDirection.Up or ArrowDirection.Down
                    ? "vertical"
                    : "horizontal";
                return $"placeholder-line {orientation}";
            }

            var baseClass = GetTriangleBaseClass();
            return index == activeIndex ? $"{baseClass} active" : baseClass;
        }

        public string GetGroupClass()
        {
            var effectiveDirection = Direction;
            var dirClass = effectiveDirection is ArrowDirection.Up or ArrowDirection.Down
                ? "arrows-in-cell dir-v" : "arrows-in-cell dir-h";

            if (UsePlaceholder)
            {
                return $"{dirClass} placeholder";
            }

            return (_direction == ArrowDirection.Hidden || !showArrows)
                ? $"{dirClass} is-invisible" : dirClass;
        }
    }

}
