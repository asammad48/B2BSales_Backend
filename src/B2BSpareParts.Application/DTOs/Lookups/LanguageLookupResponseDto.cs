namespace B2BSpareParts.Application.DTOs.Lookups;

public class LanguageLookupResponseDto : LookupItemResponseDto
{
    public string Code { get; set; } = default!;
    public bool IsRtl { get; set; }
}
