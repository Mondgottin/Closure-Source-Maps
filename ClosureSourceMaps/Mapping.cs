//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from: Test/test.proto
// Note: requires additional types generated from: google/protobuf/csharp_options.proto
using Google.ProtocolBuffers;

namespace sourcemap
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"LineMapping")]
  public partial class LineMapping : global::ProtoBuf.IExtensible
  {
    public LineMapping() {}
    
    private int _line_number = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"line_number", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int line_number
    {
      get { return _line_number; }
      set { _line_number = value; }
    }
    private int _column_position = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"column_position", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int column_position
    {
      get { return _column_position; }
      set { _column_position = value; }
    }
    private sourcemap.OriginalMapping _original_mapping = null;
    [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"original_mapping", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public sourcemap.OriginalMapping original_mapping
    {
      get { return _original_mapping; }
      set { _original_mapping = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"OriginalMapping")]
  public partial class OriginalMapping : global::ProtoBuf.IExtensible
  {
    public OriginalMapping() {}
    
    private string _original_file = "";
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"original_file", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue("")]
    public string original_file
    {
      get { return _original_file; }
      set { _original_file = value; }
    }
    private int _line_number = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"line_number", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int line_number
    {
      get { return _line_number; }
      set { _line_number = value; }
    }
    private int _column_position = default(int);
    [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"column_position", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int column_position
    {
      get { return _column_position; }
      set { _column_position = value; }
    }
    private string _identifier = "";
    [global::ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"identifier", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue("")]
    public string identifier
    {
      get { return _identifier; }
      set { _identifier = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}