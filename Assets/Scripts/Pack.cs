using System;

[Serializable]
public sealed class Pack
{
   public int msgid;
   public int id;
   public byte[] data;
   
   public Pack(){}

   public Pack(int msgid, object data, int id = 0)
   {
      this.msgid = msgid;
      this.id = id;
      this.data = Serialization.BinarySerialization.Serialization(data);
   }
   
   public T GetData<T>() where T: class, new()
   {
      return Serialization.BinarySerialization.Deserialization<T>(data);
   }
}