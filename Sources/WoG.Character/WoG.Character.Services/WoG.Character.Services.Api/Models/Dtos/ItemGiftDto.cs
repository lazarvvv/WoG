namespace WoG.Characters.Services.Api.Models.Dtos
{
    public class ItemGiftDto
    {
        public Guid ItemId { get; set; }
        public Guid GiftFrom { get; set; }
        public Guid GiftTo { get; set; }
    }
}
