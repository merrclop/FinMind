namespace FinMind.Models;

/// <summary>Місячний ліміт витрат для категорії (модель даних для розширення функціоналу).</summary>
public class BudgetLimit
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal MonthlyLimit { get; set; }
}
