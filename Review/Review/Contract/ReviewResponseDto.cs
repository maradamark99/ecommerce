namespace Review.Contract
{
    public class ReviewResponseDto
    {
        public string Comment { get; set; }
        public decimal Rating { get; set; }
        public string CustomerName { get; set; }

        public string UpdatedAt { get; set; }
    }
}