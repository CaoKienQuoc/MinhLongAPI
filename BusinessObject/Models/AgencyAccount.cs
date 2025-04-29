using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class AgencyAccount
    {
        [Key]
        public long AgencyId { get; set; }

        [Required]
        public string AgencyName { get; set; }

        //public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public int AddressId { get; set; }
        [ForeignKey("AddressId")]
        public Address Address { get; set; }

        /*public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Location Location { get; set; }*/

        public long? ManagedByEmployeeId { get; set; }

        [ForeignKey("ManagedByEmployeeId")]
        public Employee ManagedByEmployee { get; set; }

        public ICollection<AgencyAccountLevel> AgencyAccountLevels { get; set; }

        public ICollection<Contract> Contracts { get; set; }

    }
}
