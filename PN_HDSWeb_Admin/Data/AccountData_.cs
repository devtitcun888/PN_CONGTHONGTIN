namespace PN_HDSWeb_Admin.Data
{
    public class AccountData_
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string MaTruong { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; }
        public string Cap { get; set; }
        public DateTime CreatedDate
        {
            get; set;
        }
    }

    public class ApiResponseSingle_<T>
    {
        public bool Success { get; set; }           
        public string Message { get; set; }
        public T Result { get; set; }
    }

    public class GetSSOLoginUrl_
    {
        public string Result { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }



    public class SSOSessionData
    {
        public string Token { get; set; }
        public DateTime Expiry { get; set; }
        public string ReturnUrl { get; set; }
        public int RoleId { get; set; }
    }

}
