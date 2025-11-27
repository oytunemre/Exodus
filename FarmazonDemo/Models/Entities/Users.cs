namespace FarmazonDemo.Models.Entities

{

    // Model sınıfı     
    public class Users
    {

        public string Id { get; set; } = string.Empty;
        public required string Name { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public  required string Password { get; set; } = string.Empty;        
        public required string Username { get; set; } = string.Empty;
       

    }
}
