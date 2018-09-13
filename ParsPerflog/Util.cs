using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Windows;

namespace ParsPerflog
{
    class Util
    {
        public static void Serialize<Object>(Object dictionary, Stream stream)
        {
            try // try to serialize the collection to a file
            {
                using (stream)
                {
                    // create BinaryFormatter
                    BinaryFormatter bin = new BinaryFormatter();
                    // serialize the collection (EmployeeList1) to file (stream)
                    bin.Serialize(stream, dictionary);
                }
            }
            catch (IOException)
            {
            }
        }

        public static Object Deserialize<Object>(Stream stream) where Object : new()
        {
            Object ret = CreateInstance<Object>();
            try
            {
                using (stream)
                {
                    // create BinaryFormatter
                    BinaryFormatter bin = new BinaryFormatter();
                    // deserialize the collection (Employee) from file (stream)
                    stream.Seek(0, SeekOrigin.Begin);
                    ret = (Object)bin.Deserialize(stream);
                }
            }
            catch (IOException ioe)
            {
                MessageBox.Show(ioe.ToString());
            }
            return ret;
        }
        // function to create instance of T
        public static Object CreateInstance<Object>() where Object : new()
        {
            return (Object)Activator.CreateInstance(typeof(Object));
        }

        public static int Median(List<int> numbers)
        {
            var ys = numbers.OrderBy(x => x).ToList();
            double mid = (ys.Count - 1) / 2.0;
            return (ys[(int)(mid)] + ys[(int)(mid + 0.5)]) / 2;
        }
    }
}
