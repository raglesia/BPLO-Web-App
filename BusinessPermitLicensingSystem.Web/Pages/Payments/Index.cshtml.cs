using BusinessPermitLicensingSystem.Web.Profiles;
using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace BusinessPermitLicensingSystem.Web.Pages.Payments;
public class PaymentsIndexModel(ProfileService profiles, VehicleService vehicles, ILogger<PaymentsIndexModel> logger) : PageModel
{
 public string Kind {get;private set;}="stalls";
 public string Search {get;private set;}="";
 public int PageNumber {get;private set;}=1;
 public int TotalPages {get;private set;}=1;
 public IReadOnlyList<ProfileRecord> Profiles {get;private set;}=[];
 public VehicleList? Vehicles {get;private set;}
 public string? Error {get;private set;}
 public async Task OnGetAsync(string? kind,string? search,int pageNumber=1)
 {
  Kind=kind=="vehicles"?"vehicles":"stalls";
  Search=(search??"").Trim();if(Search.Length>255)Search=Search[..255];
  PageNumber=Math.Max(1,pageNumber);
  try {
   if(Kind=="stalls") { var list=await profiles.ListAsync(Search,PageNumber,HttpContext.RequestAborted);Profiles=list.Profiles;TotalPages=Math.Max(1,(list.TotalCount+ProfileService.PageSize-1)/ProfileService.PageSize); }
   else { Vehicles=await vehicles.ListAsync(Search,PageNumber,HttpContext.RequestAborted);TotalPages=Math.Max(1,(Vehicles.Total+VehicleService.PageSize-1)/VehicleService.PageSize); }
   PageNumber=Math.Min(PageNumber,TotalPages);
  }catch(Exception ex){logger.LogError(ex,"Payments workspace could not load");Error="Records could not be loaded. Try again later.";}
 }
}
