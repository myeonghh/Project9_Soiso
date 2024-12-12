using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ProjectSoiso.Model
{
    // Inventory: 검수 및 재고 관리를 위한 데이터 모델 클래스
    public class Inventory
    {
        // 상품 이름
        public string Name { get; set; }

        // 상품 이미지
        public BitmapImage Image { get; set; }

        // 상품 카테고리
        public string Category { get; set; }

        // 상품 상태 (검수 완료 여부)
        public bool State { get; set; }

        // 상품 수량
        public int Quantity { get; set; }
    }
}
