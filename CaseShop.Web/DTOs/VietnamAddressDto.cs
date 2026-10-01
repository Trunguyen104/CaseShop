namespace CaseShop.Web.DTOs;

public sealed record VietnamProvinceDto(int Code, string Name, string DivisionType);
public sealed record VietnamWardDto(int Code, string Name, string DivisionType, int ProvinceCode);
