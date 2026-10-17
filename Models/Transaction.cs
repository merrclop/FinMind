namespace FinMind.Models;

public enum TransactionType
{
    Income,
    Expense
}

/// <summary>Фінансова транзакція (дохід або витрата).</summary>
public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
