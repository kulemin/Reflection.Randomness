using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Randomness
{
    public class FromDistributionAttribute : Attribute
    {
        public IContinousDistribution Distribution { get; set; }
        public FromDistributionAttribute(Type distribution, params object[] values)
        {
            if (values.Length > 2) throw new ArgumentException("NormalDistribution must Contain 2 arguments");
            Distribution = (IContinousDistribution)Activator.CreateInstance(distribution, values);
        }
    }

    public class Generator<T>
        where T : new()
    {
        public Dictionary<PropertyInfo, IContinousDistribution> Properties = 
            new Dictionary<PropertyInfo, IContinousDistribution>();
        public Generator()
        {
            foreach (var e in typeof(T).GetProperties())
                Properties.Add(e, null);
        }

        public T Generate(Random value)
        {
            var obj = new T();

            foreach (var e in Properties)
            {
                if (e.Value == null)
                {
                    if (e.Key.SetMethod == null)
                        continue;
                    if (e.Key.GetCustomAttributes(true).OfType<FromDistributionAttribute>().ToArray().Length == 0)
                        continue;
                    var attribute = e.Key.GetCustomAttributes(true).OfType<FromDistributionAttribute>().First();
                    e.Key.SetValue(obj, attribute.Distribution.Generate(value));
                }

                else
                {
                    e.Key.SetValue(obj, e.Value.Generate(value));
                }
            }

            return obj;
        }
    }

    public static class GeneratorExtentions
    {
        public static Tuple<Generator<T>, PropertyInfo> For<T>(
            this Generator<T> generator, 
            Expression<Func<T, double>> func)
            where T : new()
        {
            if (!(func.Body is MemberExpression)) throw new ArgumentException();
            var propertyName = ((MemberExpression)func.Body).Member.Name;

            foreach (var e in generator.Properties)
            {
                if (e.Key.Name == propertyName)
                    return new Tuple<Generator<T>, PropertyInfo>(generator, e.Key);
            }

            throw new ArgumentException();
        }

        public static Generator<T> Set<T>(
            this Tuple<Generator<T>, PropertyInfo> pair, 
            IContinousDistribution distribution)
            where T : new()
        {
            PropertyInfo key = null;

            foreach (var e in pair.Item1.Properties)
            {
                if (e.Key.Name == pair.Item2.Name)
                {
                    key = e.Key;
                }
            }

            pair.Item1.Properties[key] = distribution;
            return pair.Item1;
        }
    }
}
