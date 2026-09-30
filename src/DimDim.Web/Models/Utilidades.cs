using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DimDim.Web.Models;

public static class Horario
{
    /// <summary>Horário de Brasília (UTC-3, o Brasil não usa horário de verão desde 2019).</summary>
    public static DateTime Agora() => DateTime.UtcNow.AddHours(-3);

    public static DateTime AgoraSemSegundos()
    {
        var agora = Agora();
        return new DateTime(agora.Year, agora.Month, agora.Day, agora.Hour, agora.Minute, 0);
    }
}

public static class EnumExtensoes
{
    /// <summary>Retorna o nome amigável do enum (atributo [Display]) ou o próprio nome do membro.</summary>
    public static string Nome(this Enum valor)
    {
        var membro = valor.GetType().GetMember(valor.ToString()).FirstOrDefault();
        var display = membro?.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? valor.ToString();
    }
}
