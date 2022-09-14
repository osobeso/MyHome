using System.ComponentModel.DataAnnotations;

namespace MyHome.Web.Shared
{
    public class DeviceModel
    {
        [Required]
        public string ModelId { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Device Id is too long.")]
        public string DeviceId { get; set; }
        [Required]
        public string DpsIdScope { get; set; }
        [Required]
        public string DpsEndpointName { get; set; }
        [Required]
        public string SymmetricKey { get; set; }
    }
}