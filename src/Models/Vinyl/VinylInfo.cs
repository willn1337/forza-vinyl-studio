namespace ForzaVinylStudio.Models.Vinyl;

public record VinylInfo(VinylType Type, int TypeIndex)
{
    public int Get() => (int)Type + (TypeIndex - 1);

    public override int GetHashCode()
    {
        return Get().GetHashCode();
    }

    public override string ToString()
    {
        return $"{nameof(Type)}: {Type} ({(int)Type}), {nameof(TypeIndex)}: {TypeIndex} ({Get()})";
    }
}