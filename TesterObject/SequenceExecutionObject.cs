namespace Hitachi.Tester.Sequence
{
   class SequenceExecutionObject
   {
      public SequenceExecutionObject()
      {
         TheSequence = new TestSequence();
         StartingTest = 0;
         BreakOnError = true;
         GradeName = "";
         ParseString = "";
         TableString = "";
      }

      public TestSequence TheSequence;
      public string GradeName;
      public int StartingTest;
      public bool BreakOnError;
      public string ParseString;
      public string TableString;
   }

   
}
