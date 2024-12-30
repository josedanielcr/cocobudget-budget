using web_api.Database;
using web_api.Entities;
using web_api.Enums;

namespace web_api.Extensions;

public static class TransactionExtensions
{
    public static void HandleExpenseTransaction(ApplicationDbContext dbContext, Transaction transaction, BankAccount account,
        Category category)
    {
        account.CurrentBalance -= transaction.Amount;
        if (category.GeneralCategory.Currency != account.Currency)
        {
            transaction.RequireCategoryReview = true;
            CreateTransactionEffectToCategory(dbContext, transaction, category);
            return;
        }
            
        category.AmountSpent += transaction.Amount;
        category.AmountRemaining = category.TargetAmount - category.AmountSpent;
            
        // Custom categories have a target that should decrease when a transaction is made / fixed categories don't
        if (category.GeneralCategory.CategoryType == CategoryType.Custom)
        {
            category.GeneralCategory.TargetAmount -= transaction.Amount;
        }
        
        CreateTransactionEffectToCategory(dbContext, transaction, category);
    }
    
    public static void HandleExpenseTransactionDelete(ApplicationDbContext dbContext, BankAccount account,
        Category category, TransactionCategoryEffect effect)
    {
        category.AmountSpent -= effect.Amount;
        category.AmountRemaining = category.TargetAmount - category.AmountSpent;
        
        // Custom categories have a target that should increase when a transaction is deleted / fixed categories don't
        if (category.GeneralCategory.CategoryType == CategoryType.Custom)
        {
            category.GeneralCategory.TargetAmount += effect.Amount;
        }
        
        // If the transaction was in a different currency, the effect will have a conversion rate to return the bank account to the original amount
        if (effect.ConversionRate != null)
        {
            account.CurrentBalance += effect.Amount / (effect.ConversionRate ?? 1);
        }
        else
        {
            account.CurrentBalance += effect.Amount;
        }
        
        dbContext.Categories.Update(category);
        dbContext.BankAccounts.Update(account);
    }

    /**
     * once its affected it logs into a Effect table between the transaction and the category so it can be used
     * to revert the transaction if needed and to check for conversion rates if necessary
     */
    private static void CreateTransactionEffectToCategory(ApplicationDbContext dbContext, Transaction transaction,
        Category category)
    {
        var transactionEffect = new TransactionCategoryEffect
        {
            TransactionId = transaction.Id,
            CategoryId = category.Id,
            Amount = transaction.Amount,
            ConversionRate = null
        };

        dbContext.TransactionCategoryEffects.Add(transactionEffect);
    }
}