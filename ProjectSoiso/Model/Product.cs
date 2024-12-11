namespace ProjectSoiso.Model
{
    public class Product
    // 상품 정보를 나타내는 데이터 모델 클래스
    {
        public string Name { get; set; } // 상품명 (UNIQUE)

        public string Category { get; set; } // 상품 카테고리

        public decimal Price { get; set; } // 상품 가격

        public string ImgPath { get; set; } // 상품 이미지 파일 경로

        public string QrPath { get; set; } // 상품 QR 코드 이미지 경로

        public int State { get; set; } // 현재 판매 여부 (1: 판매 중, 0: 판매 중지)

        public DateTime CreatedAt { get; set; } // 등록 일자
    }
}
