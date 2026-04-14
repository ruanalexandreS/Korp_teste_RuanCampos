using System.ComponentModel.DataAnnotations;

namespace StockService.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Código é obrigatório.")]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descrição é obrigatória.")]
    [StringLength(200, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Saldo não pode ser negativo.")]
    public int Balance { get; set; }
}