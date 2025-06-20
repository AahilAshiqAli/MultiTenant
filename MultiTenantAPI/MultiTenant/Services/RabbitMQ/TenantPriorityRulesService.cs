namespace AuthECAPI.Services.RabbitMQ
{
    public static class TenantPriorityRulesService
    {
        public static int GetPriority(string tenantId)
        {

            Console.WriteLine(tenantId);
            return tenantId switch
            {
                "143e22f7-7438-4d0a-9a74-516bd847809e" => 10,
                "F21EA9BB-FD2E-495B-8FA8-117174294B43" => 5,
                _ => 1
            };
        }

        public static string GetRoutingKey(int priority)
        {
            return priority switch
            {
                10 => "tasks.high",
                5 => "tasks.normal",
                _ => "tasks.low"
            };
        }
    }
}
