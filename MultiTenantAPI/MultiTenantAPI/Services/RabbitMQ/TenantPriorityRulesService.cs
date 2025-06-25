namespace MultiTenantAPI.Services.RabbitMQ
{
    public static class TenantPriorityRulesService
    {
        public static int GetPriority(string tenantId)
        {

            Console.WriteLine(tenantId);
            return tenantId switch
            {
                "bf843d10-90a3-48eb-b287-ff2dcdc8d2e3" => 10,
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
