// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace ZLinq.Tests
{
    public class ToArrayTests : EnumerableTests
    {
        [Fact]
        public void ToArray_CreateACopyWhenNotEmpty()
        {
            int[] sourceArray = [1, 2, 3, 4, 5];
            int[] resultArray = sourceArray.ToArray();

            Assert.NotSame(sourceArray, resultArray);
            Assert.Equal(sourceArray, resultArray);
        }

        [Fact]
        public void ToArray_UseArrayEmptyWhenEmpty()
        {
            int[] emptySourceArray = [];

            Assert.Same(emptySourceArray.ToArray(), emptySourceArray.ToArray());

            Assert.Same(emptySourceArray.Select(i => i).ToArray(), emptySourceArray.Select(i => i).ToArray());
            Assert.Same(emptySourceArray.ToList().Select(i => i).ToArray(), emptySourceArray.ToList().Select(i => i).ToArray());
            Assert.Same(new Collection<int>(emptySourceArray).Select(i => i).ToArray(), new Collection<int>(emptySourceArray).Select(i => i).ToArray());
            Assert.Same(emptySourceArray.OrderBy(i => i).ToArray(), emptySourceArray.OrderBy(i => i).ToArray());

            Assert.Same(Enumerable.Range(5, 0).ToArray(), Enumerable.Range(3, 0).ToArray());
            Assert.Same(Enumerable.Range(5, 3).Take(0).ToArray(), Enumerable.Range(3, 0).ToArray());
            Assert.Same(Enumerable.Range(5, 3).Skip(3).ToArray(), Enumerable.Range(3, 0).ToArray());

            Assert.Same(Enumerable.Repeat(42, 0).ToArray(), Enumerable.Range(84, 0).ToArray());
            Assert.Same(Enumerable.Repeat(42, 3).Take(0).ToArray(), Enumerable.Range(84, 3).Take(0).ToArray());
            Assert.Same(Enumerable.Repeat(42, 3).Skip(3).ToArray(), Enumerable.Range(84, 3).Skip(3).ToArray());
        }

        private void RunToArrayOnAllCollectionTypes<T>(T[] items, Action<T[]> validation)
        {
            validation(Enumerable.ToArray(items));
            validation(Enumerable.ToArray(new List<T>(items)));
            validation(new TestEnumerable<T>(items).ToArray());
            validation(new TestReadOnlyCollection<T>(items).ToArray());
            validation(new TestCollection<T>(items).ToArray());
        }


        [Fact]
        public void ToArray_WorkWithEmptyCollection()
        {
            RunToArrayOnAllCollectionTypes(new int[0],
                resultArray =>
                {
                    Assert.NotNull(resultArray);
                    Assert.Empty(resultArray);
                });
        }

        [Fact]
        public void ToArray_ProduceCorrectArray()
        {
            int[] sourceArray = [1, 2, 3, 4, 5, 6, 7];
            RunToArrayOnAllCollectionTypes(sourceArray,
                resultArray =>
                {
                    Assert.Equal(sourceArray.Length, resultArray.Length);
                    Assert.Equal(sourceArray, resultArray);
                });


            string[] sourceStringArray = ["1", "2", "3", "4", "5", "6", "7", "8"];
            RunToArrayOnAllCollectionTypes(sourceStringArray,
                resultStringArray =>
                {
                    Assert.Equal(sourceStringArray.Length, resultStringArray.Length);
                    for (int i = 0; i < sourceStringArray.Length; i++)
                        Assert.Same(sourceStringArray[i], resultStringArray[i]);
                });
        }

        [Fact]
        public void RunOnce()
        {
            Assert.Equal([1, 2, 3, 4, 5, 6, 7], Enumerable.Range(1, 7).RunOnce().ToArray());
            Assert.Equal(
                ["1", "2", "3", "4", "5", "6", "7", "8"],
                Enumerable.Range(1, 8).Select(i => i.ToString()).RunOnce().ToArray());
        }

        [Fact]
        public void ToArray_TouchCountWithICollection()
        {
            TestCollection<int> source = new TestCollection<int>([1, 2, 3, 4]);
            var resultArray = source.ToArray();

            Assert.Equal(source, resultArray);
            Assert.Equal(1, source.CountTouched);
        }


        [Fact]
        public void ToArray_ThrowArgumentNullExceptionWhenSourceIsNull()
        {
            int[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.ToArray());
        }

        // Generally the optimal approach. Anything that breaks this should be confirmed as not harming performance.
        [Fact(Skip = SkipReason.ICollectionCopyTo)]
        public void ToArray_UseCopyToWithICollection()
        {
            TestCollection<int> source = new TestCollection<int>([1, 2, 3, 4]);
            var resultArray = source.ToArray();

            Assert.Equal(source, resultArray);
            Assert.Equal(1, source.CopyToTouched);
        }

        [ConditionalFact(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        public void ToArray_FailOnExtremelyLargeCollection()
        {
            var thrownException = Assert.ThrowsAny<Exception>(() =>
            {
                var largeSeq = new FastInfiniteEnumerator<byte>();
                largeSeq.ToArray();
            });
            Assert.True(
                thrownException.GetType() == typeof(OverflowException) ||
                thrownException.GetType() == typeof(OutOfMemoryException),
                $"Expected OverflowException or OutOfMemoryException, got {thrownException}");
        }

        [Theory]
        [InlineData(new int[] { }, new string[] { })]
        [InlineData(new int[] { 1 }, new string[] { "1" })]
        [InlineData(new int[] { 1, 2, 3 }, new string[] { "1", "2", "3" })]
        public void ToArray_ArrayWhereSelect(int[] sourceIntegers, string[] convertedStrings)
        {
            Assert.Equal(convertedStrings, sourceIntegers.Select(i => i.ToString()).ToArray());

            Assert.Equal(sourceIntegers, sourceIntegers.Where(i => true).ToArray());
            Assert.Equal([], sourceIntegers.Where(i => false).ToArray());

            Assert.Equal(convertedStrings, sourceIntegers.Where(i => true).Select(i => i.ToString()).ToArray());
            Assert.Equal([], sourceIntegers.Where(i => false).Select(i => i.ToString()).ToArray());

            Assert.Equal(convertedStrings, sourceIntegers.Select(i => i.ToString()).Where(s => s is not null).ToArray());
            Assert.Equal([], sourceIntegers.Select(i => i.ToString()).Where(s => s is null).ToArray());
        }

        [Theory]
        [InlineData(new int[] { }, new string[] { })]
        [InlineData(new int[] { 1 }, new string[] { "1" })]
        [InlineData(new int[] { 1, 2, 3 }, new string[] { "1", "2", "3" })]
        public void ToArray_ListWhereSelect(int[] sourceIntegers, string[] convertedStrings)
        {
            var sourceList = new List<int>(sourceIntegers);

            Assert.Equal(convertedStrings, sourceList.Select(i => i.ToString()).ToArray());

            Assert.Equal(sourceList, sourceList.Where(i => true).ToArray());
            Assert.Equal([], sourceList.Where(i => false).ToArray());

            Assert.Equal(convertedStrings, sourceList.Where(i => true).Select(i => i.ToString()).ToArray());
            Assert.Equal([], sourceList.Where(i => false).Select(i => i.ToString()).ToArray());

            Assert.Equal(convertedStrings, sourceList.Select(i => i.ToString()).Where(s => s is not null).ToArray());
            Assert.Equal([], sourceList.Select(i => i.ToString()).Where(s => s is null).ToArray());
        }

        [Fact]
        public void SameResultsRepeatCallsFromWhereOnIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.ToArray(), q.ToArray());
        }

        [Fact]
        public void SameResultsRepeatCallsFromWhereOnStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.ToArray(), q.ToArray());
        }

        [Fact]
        public void SameResultsButNotSameObject()
        {
            var qInt = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                       where x > int.MinValue
                       select x;

            var qString = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                          where !string.IsNullOrEmpty(x)
                          select x;

            Assert.NotSame(qInt.ToArray(), qInt.ToArray());
            Assert.NotSame(qString.ToArray(), qString.ToArray());
        }

        [Fact]
        public void EmptyArraysSameObject()
        {
            // .NET Core returns the instance as an optimization.
            // see https://github.com/dotnet/corefx/pull/2401.
            Assert.True(ReferenceEquals(Enumerable.Empty<int>().ToArray(), Enumerable.Empty<int>().ToArray()));

            var array = new int[0];
            Assert.NotSame(array, array.ToArray());
        }

        [Fact]
        public void SourceIsEmptyICollectionT()
        {
            int[] source = [];

            ICollection<int> collection = source as ICollection<int>;

            Assert.Empty(source.ToArray());
            Assert.Empty(collection.ToArray());
        }

        [Fact]
        public void SourceIsICollectionTWithFewElements()
        {
            int?[] source = [-5, null, 0, 10, 3, -1, null, 4, 9];
            int?[] expected = [-5, null, 0, 10, 3, -1, null, 4, 9];

            ICollection<int?> collection = source as ICollection<int?>;

            Assert.Equal(expected, source.ToArray());
            Assert.Equal(expected, collection.ToArray());
        }

        [Fact]
        public void SourceNotICollectionAndIsEmpty()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(-4, 0);

            Assert.Null(source as ICollection<int>);

            Assert.Empty(source.ToArray());
        }

        [Fact]
        public void SourceNotICollectionAndHasElements()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(-4, 10);
            int[] expected = [-4, -3, -2, -1, 0, 1, 2, 3, 4, 5];

            Assert.Null(source as ICollection<int>);

            Assert.Equal(expected, source.ToArray());
        }

        [Fact]
        public void SourceNotICollectionAndAllNull()
        {
            IEnumerable<int?> source = RepeatedNullableNumberGuaranteedNotCollectionType(null, 5);
            int?[] expected = [null, null, null, null, null];

            Assert.Null(source as ICollection<int>);

            Assert.Equal(expected, source.ToArray());
        }

        [Fact]
        public void ConstantTimeCountPartitionSelectSameTypeToArray()
        {
            var source = Enumerable.Range(0, 100).Select(i => i * 2).Skip(1).Take(5);
            Assert.Equal([2, 4, 6, 8, 10], source.ToArray());
        }

        [Fact]
        public void ConstantTimeCountPartitionSelectDiffTypeToArray()
        {
            var source = Enumerable.Range(0, 100).Select(i => i.ToString()).Skip(1).Take(5);
            Assert.Equal(["1", "2", "3", "4", "5"], source.ToArray());
        }

        [Fact]
        public void ConstantTimeCountEmptyPartitionSelectSameTypeToArray()
        {
            var source = Enumerable.Range(0, 100).Select(i => i * 2).Skip(1000);
            Assert.Empty(source.ToArray());
        }

        [Fact]
        public void ConstantTimeCountEmptyPartitionSelectDiffTypeToArray()
        {
            var source = Enumerable.Range(0, 100).Select(i => i.ToString()).Skip(1000);
            Assert.Empty(source.ToArray());
        }

        [Fact]
        public void NonConstantTimeCountPartitionSelectSameTypeToArray()
        {
            var source = NumberRangeGuaranteedNotCollectionType(0, 100).OrderBy(i => i).Select(i => i * 2).Skip(1).Take(5);
            Assert.Equal([2, 4, 6, 8, 10], source.ToArray());
        }

        [Fact]
        public void NonConstantTimeCountPartitionSelectDiffTypeToArray()
        {
            var source = NumberRangeGuaranteedNotCollectionType(0, 100).OrderBy(i => i).Select(i => i.ToString()).Skip(1).Take(5);
            Assert.Equal(["1", "2", "3", "4", "5"], source.ToArray());
        }

        [Fact]
        public void NonConstantTimeCountEmptyPartitionSelectSameTypeToArray()
        {
            var source = NumberRangeGuaranteedNotCollectionType(0, 100).OrderBy(i => i).Select(i => i * 2).Skip(1000);
            Assert.Empty(source.ToArray());
        }

        [Fact]
        public void NonConstantTimeCountEmptyPartitionSelectDiffTypeToArray()
        {
            var source = NumberRangeGuaranteedNotCollectionType(0, 100).OrderBy(i => i).Select(i => i.ToString()).Skip(1000);
            Assert.Empty(source.ToArray());
        }

        [Theory]
        [MemberData(nameof(ToArrayShouldWorkWithSpecialLengthLazyEnumerables_MemberData))]
        public void ToArrayShouldWorkWithSpecialLengthLazyEnumerables(int length)
        {
            Debug.Assert(length >= 0);

            var range = Enumerable.Range(0, length);
            var lazyEnumerable = ForceNotCollection(range); // We won't go down the IIListProvider path
            Assert.Equal(range, lazyEnumerable.ToArray());
        }

        // Consider that two very similar enums is not unheard of, if e.g. two assemblies map the
        // same external source of numbers (codes, response codes, colour codes, etc.) to values.
        private enum Enum0
        {
            First,
            Second,
            Third
        }

        private enum Enum1
        {
            First,
            Second,
            Third
        }

        [Fact]
        public void ToArray_Cast()
        {
            Enum0[] source = [Enum0.First, Enum0.Second, Enum0.Third];
            var cast = source.Cast<Enum1>();
            // Assert.IsType<Enum0[]>(cast); // ZLinq don't support implicit cast to IEnumerable
            var castArray = cast.ToArray();
            Assert.IsType<Enum1[]>(castArray);
            Assert.Equal([Enum1.First, Enum1.Second, Enum1.Third], castArray);
        }

        public static IEnumerable<object[]> ToArrayShouldWorkWithSpecialLengthLazyEnumerables_MemberData()
        {
            // Return array sizes that should be small enough not to OOM
            int MaxPower = PlatformDetection.IsBrowser ? 15 : 18;
            yield return [1];
            yield return [2];
            for (int i = 2; i <= MaxPower; i++)
            {
                yield return [(i << i) - 1];
                yield return [(i << i)];
                yield return [(i << i) + 1];
            }
        }
    }
}
