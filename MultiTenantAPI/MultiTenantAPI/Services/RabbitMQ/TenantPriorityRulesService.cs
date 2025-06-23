namespace MultiTenantAPI.Services.RabbitMQ
{
    public static class TenantPriorityRulesService
    {
        public static int GetPriority(string tenantId)
        {

            Console.WriteLine(tenantId);
            return tenantId switch
            {
                "30c98ac5-aa99-4cde-a178-71e3dd2cbdd7" => 10,
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
