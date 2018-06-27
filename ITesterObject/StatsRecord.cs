using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Hitachi.Tester.Module
{
   public class StatsRecord
   {
      public StatsRecord()
      {
         RecordTime = DateTime.Now;
         TotalCount = 0;
         Value = 0;
         RecordIndex = 0;
      }

      public StatsRecord(Int64 total, Int64 value, Int64 index)
      {
         RecordTime = DateTime.Now;
         TotalCount = total;
         Value = value;
         RecordIndex = index;
      }

      public DateTime RecordTime;
      public Int64 TotalCount;
      public Int64 Value;
      public Int64 RecordIndex;

      public void Read(BinaryReader br)
      {
         RecordTime = new DateTime(br.ReadInt64());
         TotalCount = br.ReadInt64();
         Value = br.ReadInt64();
         RecordIndex = br.ReadInt64();
      }

      public void Write(BinaryWriter bw)
      {
         bw.Write(RecordTime.Ticks);
         bw.Write(TotalCount);
         bw.Write(Value);
         bw.Write(RecordIndex);
      }

      public static Int64 RecordSize
      {
         get
         {
            return 4 * sizeof(Int64);
         }
      }

 




   }
}
