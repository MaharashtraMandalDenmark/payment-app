using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace PayMMD.Pages
{
    public partial class Index
    { 
        [Inject]
        private HttpClient _httpClient {get; set;}= default!;
        private Activities[]? activities;

        protected override async Task OnInitializedAsync()
        {
            activities = await _httpClient.GetFromJsonAsync<Activities[]>("static-data/activities.json");
        }

        public class Activities
        {
            public string? Activity { get; set; }

            public DateTime? Date { get; set; }

            public int? MemberShipFamily { get; set; }
            public int? MemberShipAdult { get; set; }
            public int? MemberShipKid { get; set; }
            public int? NonMemberShipFamily { get; set; }
            public int? NonMemberShipAdult { get; set; }
            public int? NonMemberShipKid { get; set; }
        }

    }
}