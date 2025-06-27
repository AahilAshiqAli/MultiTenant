namespace MultiTenantAPI.Services.RabbitMQ
{
    public static class TenantPriorityRulesService
    {
        public static int GetPriority(string tenantId)
        {

            Console.WriteLine(tenantId);
            return tenantId switch
            {
                "bf843d10-90a3-48eb-b287-ff2dcdc8d2e3" => 4,
                "F21EA9BB-FD2E-495B-8FA8-117174294B43" => 2,
                _ => 1
            };
        }

        public static string GetRoutingKey(int priority)
        {
            return priority switch
            {
                4 => "tasks.high",
                2 => "tasks.normal",
                1 => "tasks.low",
                _ => throw new NotImplementedException()
            };
        }
    }
}
