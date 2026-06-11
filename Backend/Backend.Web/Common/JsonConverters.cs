using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Core.Common.ValueObjects;

namespace Backend.Web.Common;

public class DddJsonConverter : JsonConverter<Ddd>
{
    public override Ddd Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new Ddd(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, Ddd value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Valor);
    }
}

public class DdiJsonConverter : JsonConverter<Ddi>
{
    public override Ddi Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new Ddi(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, Ddi value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Valor);
    }
}

public class DocumentoJsonConverter : JsonConverter<Documento>
{
    public override Documento? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        if (stringValue == null) return null;

        var cleanValue = new string(stringValue.Where(char.IsLetterOrDigit).ToArray());

        if (cleanValue.Length == 11)
            return new Cpf(stringValue);
        if (cleanValue.Length == 14)
            return new Cnpj(stringValue);

        return new DocumentoGenerico(stringValue);
    }

    public override void Write(Utf8JsonWriter writer, Documento value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Valor);
    }
}

public class CpfJsonConverter : JsonConverter<Cpf>
{
    public override Cpf? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue != null ? new Cpf(stringValue) : null;
    }

    public override void Write(Utf8JsonWriter writer, Cpf value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Valor);
    }
}

public class CnpjJsonConverter : JsonConverter<Cnpj>
{
    public override Cnpj? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue != null ? new Cnpj(stringValue) : null;
    }

    public override void Write(Utf8JsonWriter writer, Cnpj value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Valor);
    }
}

public class DocumentoGenericoJsonConverter : JsonConverter<DocumentoGenerico>
{
    public override DocumentoGenerico? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue != null ? new DocumentoGenerico(stringValue) : null;
    }

    public override void Write(Utf8JsonWriter writer, DocumentoGenerico value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Valor);
    }
}
