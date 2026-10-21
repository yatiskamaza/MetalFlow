namespace MetalFlow.Dominio.Enumeraciones;

/// <summary>
/// Situación regulatoria del material (RD-01).
/// El sistema no cataloga como residuo el retal geométrico reutilizable,
/// conforme a la Ley 7/2022 y al Reglamento (UE) 333/2011.
/// </summary>
public enum ClasificacionRegulatoria : byte
{
    Excedente = 1,
    Subproducto = 2
}
