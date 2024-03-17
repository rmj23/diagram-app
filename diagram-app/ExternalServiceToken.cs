namespace diagram_app
{
    public class ExternalServiceToken
    {
        public int Id { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public bool IsPending { get; set; }
        public string State { get; set; } = string.Empty;
    }
}