using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    public class Person
    {
        public int PersonId { get; set; }
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
    }

    public interface IPersonRepository
    {
        void CreateTable();
        void DropTable();
        bool TableExists();
        bool TableExists(IDbConnection conn);

        void Insert(Person p);
        IEnumerable<Person> GetAll();

        IEnumerable<Person> GetByName(string name);
        Person GetSingletonByName(string name);

        Person FindByBirthday(DateTime birthday);

        Person Get(int personId);

        int GetCountByName(string name);
        bool GetExistsByName(string name);

        bool TryGet(int personId, out Person result);
        bool TryGetByName(string name, out Person result);

        void Update(Person person);
        void Delete(Person person);

        void Insert(Person person, IDbTransaction txn);
    }

    public interface IPersonRepositoryWithCreate
    {
        void CreateTable();
        void DropTable();
        bool TableExists();

        Person Get(int personId);

        void Create(Person person);
        int CreateReturningInt(Person person);
    }

    public interface IRepositoryWithBogusMethodName
    {
        string DoSomeStuff();
    }

    public interface IConstructorInjectedRepository
    {
        void CreateTable();
        void DropTable();
        bool TableExists();

        void Insert(ConstructorInjected p);
        IEnumerable<ConstructorInjected> GetAll();

        IEnumerable<ConstructorInjected> GetByName(string name);
        ConstructorInjected GetSingletonByName(string name);

        ConstructorInjected Get(int personId);
        bool TryGet(int personId, out ConstructorInjected result);
        bool TryGetByName(string name, out ConstructorInjected result);

        void Update(ConstructorInjected person);
        void Delete(ConstructorInjected person);

        void Insert(ConstructorInjected person, IDbTransaction txn);
    }

    public class ConstructorInjected
    {
        private readonly int _personId;
        private readonly string _name;
        private readonly DateTime _birthday;

        public ConstructorInjected(int personId, string name, DateTime birthday)
        {
            _personId = personId;
            _name = name;
            _birthday = birthday;
        }

        public int PersonId
        {
            get { return _personId; }
        }

        public string Name
        {
            get { return _name; }
        }

        public DateTime Birthday
        {
            get { return _birthday; }
        }
    }

    public class ColorThing
    {
        private readonly int _id;
        private readonly Color _color;
        private readonly BigColor _bigColor;
        private readonly LittleColor _littleColor;
        private readonly Color? _nullableColor;

        public ColorThing(int id, Color color, BigColor bigColor, LittleColor littleColor, Color? nullableColor)
        {
            _id = id;
            _color = color;
            _bigColor = bigColor;
            _littleColor = littleColor;
            _nullableColor = nullableColor;
        }

        public int Id { get { return _id; }}
        public Color Color { get { return _color; } }
        public BigColor BigColor { get { return _bigColor; } }
        public LittleColor LittleColor { get { return _littleColor; } }
        public Color? NullableColor { get { return _nullableColor; } }
    }

    public interface IColorThingRepo
    {
        void CreateTable();
        void DropTable();
        bool TableExists();
        void Insert(ColorThing t);
        ColorThing Get(int id);
    }

    public enum Color
    {
        Red,
        White,
        Blue,
    }

    public enum BigColor : long
    {
        BigRed,
        BigWhite,
        BigBlue,
    }

    public enum LittleColor : byte
    {
        LittleRed,
        LittleWhite,
        LittleBlue,
    }


}

