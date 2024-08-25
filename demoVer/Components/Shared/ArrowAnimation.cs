using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading;
using System.Threading.Tasks;
namespace demoVer.Shared
{
    public class ArrowAnimationBase : ComponentBase, IDisposable
    {
        #region arrowAnimation
        protected ElementReference leftRef;
        protected ElementReference rightRef;
        
        protected ElementReference tower;
        protected ElementReference NTN;
        protected ElementReference load;
        protected ElementReference battery;
        protected ElementReference grid_container;

        protected int arrowLeft;
        protected int arrowTop;
        protected int activeIndex = 0;
        protected bool showArrows = false;

        protected class triangle_group
        {
            public int arrowLeft;
            public int arrowTop;
            public int activeIndex;
            public bool showArrows;
            public bool position_OK;

            public triangle_group()
            {
                this.activeIndex = 0;
                this.showArrows = false;
                this.position_OK = false;
            }
            public void set_triGroup_postion(int left, int top)
            {
                this.arrowLeft = left;
                this.arrowTop = top;
                this.position_OK = true;
            }
            public void show_arrows()
            {
                if(this.position_OK){
                    this.showArrows = true;
                }
            }
            public void clear_arrows()
            {
                this.showArrows = false;
            }
            public bool get_showArrowFlag()
            {
                return this.showArrows;
            }
            public int get_arrowLeft()
            {
                return this.arrowLeft;
            }
            public int get_arrowTop()
            {
                return this.arrowTop;
            }
            public string GetTriGroupStyle()
            {
                return $"left:{this.get_arrowLeft()}px; top:{this.get_arrowTop()}px;";
            }
        }

        protected triangle_group triGroup1 = new triangle_group();
        protected triangle_group triGroup2 = new triangle_group();
        protected triangle_group triGroup3 = new triangle_group();

        protected System.Threading.Timer? timer;

        [Inject] protected IJSRuntime JS {get; set; } = default;

        // DomRect 物件用來接 JS 回傳的資料
        protected class DomRect
        {
            public double Bottom { get; set; }
            public double Height { get; set; }
            public double Left { get; set; }
            public double Right { get; set; }
            public double Top { get; set; }
            public double Width { get; set; }
        }

        protected async Task UpdateArrowPosition()
        {
            var leftRect = await JS.InvokeAsync<DomRect>("getBoundingClientRect", tower, grid_container);
            var rightRect = await JS.InvokeAsync<DomRect>("getBoundingClientRect", NTN, grid_container);
            arrowLeft = (int)((leftRect.Right + rightRect.Left) / 2) - 8; //if no "-8", the first triangle will appear at middle
            arrowTop = (int)((leftRect.Top + rightRect.Top) / 2);
            Console.WriteLine($"left : {arrowLeft}, top : {arrowTop}");
            triGroup1.set_triGroup_postion(arrowLeft, arrowTop);
            triGroup1.show_arrows();


            leftRect = await JS.InvokeAsync<DomRect>("getBoundingClientRect", NTN, grid_container);
            rightRect = await JS.InvokeAsync<DomRect>("getBoundingClientRect", load, grid_container);
            arrowLeft = (int)((leftRect.Right + rightRect.Left) / 2);
            arrowTop = (int)((leftRect.Top + rightRect.Top) / 2);
            triGroup2.set_triGroup_postion(arrowLeft, arrowTop);
            triGroup2.show_arrows();


            leftRect = await JS.InvokeAsync<DomRect>("getBoundingClientRect", NTN, grid_container);
            rightRect = await JS.InvokeAsync<DomRect>("getBoundingClientRect", battery, grid_container);
            arrowLeft = (int)((leftRect.Right + rightRect.Left) / 2);
            arrowTop = (int)((leftRect.Top + rightRect.Top) / 2);
            triGroup3.set_triGroup_postion(arrowLeft, arrowTop);
            triGroup3.show_arrows();
        }

        protected void StartTimer()
        {
            timer = new System.Threading.Timer(_ =>
            {   
                Console.WriteLine("timer");
                if(triGroup1.get_showArrowFlag() || triGroup2.get_showArrowFlag() || triGroup3.get_showArrowFlag())
                {
                    if(triGroup1.get_showArrowFlag())
                    {
                        triGroup1.activeIndex = (triGroup1.activeIndex + 1) % 3;
                        Console.WriteLine("group1.index : " + triGroup1.activeIndex);
                    }
                    if(triGroup2.get_showArrowFlag())
                    {
                        triGroup2.activeIndex = (triGroup2.activeIndex + 1) % 3;
                        Console.WriteLine("group2.index : " + triGroup2.activeIndex);
                    }
                    if(triGroup3.get_showArrowFlag())
                    {
                        triGroup3.activeIndex = (triGroup3.activeIndex + 1) % 3;
                        Console.WriteLine("group3.index : " + triGroup3.activeIndex);
                    }
                    InvokeAsync(StateHasChanged);
                }
            }, null, 0, 1000);
        }
        public void Dispose()
        {
            timer?.Dispose();
        }
        #endregion //arrowAnimation
    }
}