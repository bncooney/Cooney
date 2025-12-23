namespace TestNamespace
{
	public class TestClass
	{
		public void MyMethod() { }

		public void Caller()
		{
			MyMethod();
			MyMethod();
		}
	}
}
