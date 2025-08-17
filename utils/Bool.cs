using System.Threading;

namespace Chizl.ThreadSupport
{
    /// <summary>
    /// Thread Safe Boolean value..<br/>
    /// <code>
    /// static Bool _myVar = new Bool(true);
    /// ...
    /// var prevBool = _myVar.SetVal(false);
    /// Console.WriteLine($"The old value was: {prevBool}.  The new value is: {_myVar}");
    /// </code>
    /// </summary>
    public sealed class Bool
    {
        // Using long instead of int, because netstandard2.0
        // and Interlocked.Read(), doesn't have a "ref int" option.
        private long _boolValue;

        /// <summary>
        /// Thread Safe Boolean value..<br/>
        /// <code>
        /// static Bool _myVar = new Bool(true);
        /// or
        /// static Bool _myVar = new Bool();
        /// ...
        /// var prevBool = _myVar.SetVal(false);
        /// Console.WriteLine($"The old value was: {prevBool}.  The new value is: {_myVar}");
        /// </code>
        /// </summary>
        /// <param name="defaultValue">(Optional) Default: false</param>
        public Bool(bool defaultValue = false) { _boolValue = defaultValue ? 1 : 0; }

        /// <summary>
        /// Developer Preferances Method as alternative to SetVal(true).<br/>
        /// Changes Bool value to True.
        /// <code>
        /// var prevBool = _myVar.SetTrue();
        /// or
        /// if (!_myVar.SetTrue())
        ///     Console.WriteLine($"Previous value was false, but now set to {_myVar}.");
        /// </code>
        /// </summary>
        /// <param name="boolValue">New Boolean value.</param>
        /// <returns>Previous Boolean value before SetTrue().</returns>
        public bool SetTrue() => SetVal(true);

        /// <summary>
        /// Developer Preferances Method as alternative to SetVal(false).<br/>
        /// Changes Bool value to False.
        /// <code>
        /// var prevBool = _myVar.SetFalse();
        /// or
        /// if (!_myVar.SetFalse())
        ///     Console.WriteLine($"Previous value was false, but now set to {_myVar}.");
        /// </code>
        /// </summary>
        /// <param name="boolValue">New Boolean value.</param>
        /// <returns>Previous Boolean value before SetTrue.</returns>
        public bool SetFalse() => SetVal(false);

        /// <summary>
        /// Can change the Boolean value.
        /// <code>
        /// var prevBool = _myVar.SetVal(true);
        /// or
        /// if (!_myVar.SetVal(true))
        ///     Console.WriteLine($"Previous value was false, but now set to {_myVar}.");
        /// </code>
        /// </summary>
        /// <param name="boolValue">New Boolean value.</param>
        /// <returns>Previous Boolean value before SetVal().</returns>
        public bool SetVal(bool boolValue) => Interlocked.Exchange(ref _boolValue, (boolValue ? 1 : 0)) == 1;

        /// <summary>
        /// Default, when a property isn't assigned, allowing Bool class to be used like a variable, returning if it's True or False.
        /// <code>
        /// static Bool _myVar = new Bool(true);
        /// ...
        /// Console.WriteLine($"_myVar = {_myVar}");
        /// 
        /// Returns: "_myVar = true"
        /// </code>
        /// </summary>
        public static implicit operator bool(Bool obj)
        {
            return obj?.IsTrue ?? false;
        }

        /// <summary>
        /// To display in strings, this override will show up correctly.
        /// <code>
        /// static Bool _myVar = new Bool(false);
        /// ...
        /// if (!_myVar)
        ///     Console.WriteLine($"_myVar = {_myVar}");
        ///     
        /// Returns: "_myVar = false"
        /// </code>
        /// </summary>
        /// <returns></returns>
        public override string ToString() => IsTrue.ToString();

        /// <summary>
        /// Private property to return current state of bool value.
        /// <code>
        /// Console.WriteLine($"The property is: {_myVar.IsTrue}");
        /// </code>
        /// </summary>
        private bool IsTrue => Interlocked.Read(ref _boolValue) == 1;
    }
}
