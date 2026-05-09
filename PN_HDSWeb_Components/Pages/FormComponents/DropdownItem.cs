namespace PN_HDSWeb_Components.Pages.FormComponents;

/// <summary>
/// Item model dùng cho SearchableDropdown&lt;TValue&gt;.
/// Tách ra file riêng để tránh lỗi compiler "already contains a definition" khi dùng @typeparam.
/// </summary>
public class DropdownItem<TValue>
{
    public TValue Value { get; set; } = default!;
    public string Text { get; set; } = "";
    public string? Group { get; set; }
}
