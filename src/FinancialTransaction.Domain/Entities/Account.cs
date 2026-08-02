using FinancialTransaction.Domain.Common;

namespace FinancialTransaction.Domain.Entities;

public class Account : Entity
{
    public string Number { get; private set; }

    private Account(Guid id, string number) : base(id)
    {
        Number = number;
    }

    public static Account Create(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("O número da conta é obrigatório.", nameof(number));
        }

        return new Account(Guid.NewGuid(), number.Trim());
    }
}
