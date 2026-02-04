namespace FarmazonDemo.Models;

public class IyzicoSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";
    public bool IsSandbox { get; set; } = true;
    public string CallbackUrl { get; set; } = string.Empty;
    public string ThreeDSCallbackUrl { get; set; } = string.Empty;
}
