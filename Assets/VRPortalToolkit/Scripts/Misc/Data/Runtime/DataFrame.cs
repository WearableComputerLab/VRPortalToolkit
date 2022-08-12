using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Data
{
    // Get Type?
    // Add IEnumerable
    // 

    public class DataFrame
    {
        private int _columnCount = 0;
        public int ColumnCount {
            get => _columnCount;
            set {
                if (value < 0) value = 0;

                if (_columnCount != value)
                {
                    ExpandCapacity(value, _rowCount);

                    for (int x = value; x < _columnCount; x++)
                    {
                        _columns[x].Clear();

                        for (int y = 0; y < _rowCount; y++)
                            _elements[x, y].value = null;
                    }

                    _columnCount = value;
                }
            }
        }

        private int _rowCount = 0;
        public int RowCount {
            get => _rowCount;
            set {
                if (value < 0) value = 0;

                if (_rowCount != value)
                {
                    ExpandCapacity(_columnCount, value);

                    for (int x = 0; x < _columnCount; x++)
                    {
                        _columns[x].rowByValue?.Clear();

                        for (int y = value; y < _rowCount; y++)
                            _elements[x, y].value = null;
                    }

                    _rowCount = value;
                }
            }
        }

        private Column[] _columns;
        private Element[,] _elements;

        public int ColumnCapacity {
            get => _elements.GetLength(0);
            set => SetCapacity(value, _elements.GetLength(1));
        }

        public int RowCapacity {
            get => _elements.GetLength(1);
            set => SetCapacity(_elements.GetLength(0), value);
        }

        public object this[string column, int row] {
            get => Get(column, row);
            set => Set(column, row, value);
        }

        public object this[int column, int row] {
            get => Get(column, row);
            set => Set(column, row, value);
        }

        private Dictionary<string, int> _nameToColumn;
        private List<int> found = new List<int>();

        public DataFrame() : this(2) { }

        public DataFrame(int columnCapacity) : this(columnCapacity, 2) { }

        public DataFrame(int columnCapacity, int rowCapcity)
        {
            _elements = new Element[columnCapacity, rowCapcity];
            _columns = new Column[columnCapacity];
        }

        #region Capacity

        private void ExpandCapacity(int columnCount, int rowCount)
        {
            int columnCapacity = ColumnCapacity,
                rowCapacity = RowCapacity;

            if (columnCapacity < columnCount || rowCapacity < rowCount)
            {
                if (columnCapacity <= 0 && columnCount > 0) columnCapacity = 1;

                while (columnCapacity <= columnCount)
                    columnCapacity *= 2;

                if (rowCapacity <= 0 && rowCount > 0) rowCapacity = 1;

                while (rowCapacity <= rowCount)
                    rowCapacity *= 2;

                SetCapacity(columnCapacity, rowCapacity);
            }
        }

        public void SetCapacity(int columnCapacity, int rowCapacity)
        {
            if (columnCapacity < 0) columnCapacity = 0;
            if (rowCapacity < 0) rowCapacity = 0;

            int currentColumnCapacity = ColumnCapacity,
                currentRowCapacity = RowCapacity;

            if (currentColumnCapacity == columnCapacity && currentRowCapacity == rowCapacity)
                return;

            Element[,] newElements = new Element[columnCapacity, rowCapacity];

            if (_columnCount < columnCapacity)
            {
                for (int x = columnCapacity; x < _columnCount; x++)
                {
                    Column column = _columns[x];

                    if (column.name != null)
                        _nameToColumn.Remove(column.name);

                    _columns[x].Clear();
                }

                _columnCount = columnCapacity;
            }

            if (_rowCount < rowCapacity)
            {
                _rowCount = rowCapacity;

                for (int x = 0; x < columnCapacity; x++)
                    _columns[x].ClearCache();
            }

            for (int x = 0; x < _columnCount; x++)
                for (int y = 0; y < _rowCount; y++)
                    newElements[x, y] = _elements[x, y];

            _elements = newElements;

            if (currentColumnCapacity != columnCapacity)
            {
                Column[] newColumns = new Column[columnCapacity];

                for (int x = 0; x < _columnCount; x++)
                    newColumns[x] = _columns[x];

                _columns = newColumns;
            }
        }

        #endregion

        #region Column Name

        public int GetColumnIndex(string columnName)
        {
            TryGetColumnIndex(columnName, out int index);
            return index;
        }

        public bool TryGetColumnIndex(string columnName, out int columnIndex)
        {
            if (_nameToColumn.TryGetValue(columnName, out columnIndex)) return true;
            columnIndex = -1;
            return false;
        }

        public string GetColumnName(int columnIndex)
        {
            TryGetColumnName(columnIndex, out string name);
            return name;
        }

        public bool TryGetColumnName(int columnIndex, out string nameIndex)
        {
            if (columnIndex < 0 || columnIndex >= _columnCount) throw new IndexOutOfRangeException();

            Column actualColumn = _columns[columnIndex];

            if (actualColumn.name != null)
            {
                nameIndex = actualColumn.name;
                return true;
            }

            nameIndex = null;
            return false;
        }

        public void ClearColumnName(int columnIndex) => SetColumnName(columnIndex, null);

        public void SetColumnName(int columnIndex, string columnName)
        {
            if (columnIndex < 0 || columnIndex >= _columnCount) throw new IndexOutOfRangeException();

            Column actualColumn = _columns[columnIndex];

            if (actualColumn.name != null)
                _nameToColumn.Remove(actualColumn.name);

            _columns[columnIndex].name = columnName;

            if (columnName != null)
                _nameToColumn.Add(columnName, columnIndex);
        }

        public int GetOrAddColumn(string column)
        {
            if (TryGetColumnIndex(column, out int index))
            {
                AddColumn(column);
                index = _columnCount - 1;
            }

            return index;
        }

        #endregion

        #region Add Column

        public bool AddColumn(string column)
        {
            if (column == null || !_nameToColumn.ContainsKey(column))
            {
                AddColumn();
                SetColumnName(_columnCount - 1, column);

                return true;
            }

            return false;
        }

        public void AddColumn() => ExpandCapacity(++_columnCount, _rowCount);

        public bool AddColumn<T>(string column, IEnumerable<T> collection)
        {
            if (column == null || !_nameToColumn.ContainsKey(column))
            {
                AddColumn(collection);
                SetColumnName(_columnCount - 1, column);

                return true;
            }

            return false;
        }

        public void AddColumn<T>(IEnumerable<T> collection)
        {
            int x = _columnCount, y = 0;

            AddColumn();

            foreach (T value in collection)
                Add(x, y++, value);
        }

        public bool InsertColumn(int columnIndex, string columnName)
        {
            if (columnName == null || !_nameToColumn.ContainsKey(columnName))
            {
                InsertColumn(columnIndex);
                SetColumnName(columnIndex, columnName);
                return true;
            }

            return false;
        }

        public void InsertColumn(int column)
        {
            if (column < 0 || column > _columnCount) throw new IndexOutOfRangeException();

            AddColumn();

            for (int i = column, j = column + 1; j < _columnCount; i = j++)
            {
                Column actualColumn = _columns[j] = _columns[i];

                if (actualColumn.name != null)
                    _nameToColumn.Add(actualColumn.name, j);

                for (int y = 0; y < _rowCount; y++)
                    _elements[j, y] = _elements[i, y];
            }

            _columns[column].Clear();
            for (int y = 0; y < _rowCount; y++)
                _elements[column, y].value = null;
        }

        public bool InsertColumn<T>(int columnIndex, string columnName, IEnumerable<T> collection)
        {
            if (columnName == null || !_nameToColumn.ContainsKey(columnName))
            {
                InsertColumn(columnIndex, collection);
                SetColumnName(_columnCount - 1, columnName);

                return true;
            }

            return false;
        }

        public void InsertColumn<T>(int column, IEnumerable<T> collection)
        {
            int y = 0;

            InsertColumn(column);

            foreach (T value in collection)
                Add(column, y++, value);
        }

        #endregion

        #region Add Row

        public void AddRow() => ExpandCapacity(_columnCount, ++_rowCount);

        public void AddRow<T>(IEnumerable<T> collection)
        {
            int x = 0, y = _rowCount;

            AddRow();

            foreach (T value in collection)
                Add(x++, y, value);
        }

        public void InsertRow(int row)
        {
            if (row < 0 || row > _rowCount) throw new IndexOutOfRangeException();

            AddRow();

            for (int i = row, j = row + 1; j < _rowCount; i = j++)
                for (int x = 0; x < _columnCount; x++)
                    _elements[x, j] = _elements[x, i];

            for (int x = 0; x < _columnCount; x++)
                _elements[x, row].value = null;
        }

        public void InsertRow<T>(int row, ICollection<T> collection)
        {
            int x = 0;

            InsertRow(row);

            foreach (T value in collection)
                Add(x++, row, value);
        }

        #endregion

        #region Add

        public void Add(string column, int row, object value) => Add(GetOrAddColumn(column), row, value);

        public void Add(int column, int row, object value)
        {
            if (column >= _columnCount || row >= _rowCount)
            {
                ExpandCapacity(column, row);
                ColumnCount = Mathf.Max(column, _columnCount);
                RowCount = Mathf.Max(row, _rowCount);
            }

            Set(column, row, value);
        }

        #endregion

        #region Remove

        public bool RemoveColumn(string column)
        {
            if (TryGetColumnIndex(column, out int index))
            {
                RemoveColumn(index);
                return true;
            }

            return false;
        }

        public void RemoveColumn(int column)
        {
            if (column < 0 || column >= _columnCount) throw new IndexOutOfRangeException();

            Column actualColumn = _columns[column];

            if (actualColumn.name != null)
                _nameToColumn.Remove(actualColumn.name);

            for (int i = column, j = column + 1; j < _columnCount; i = j++)
            {
                actualColumn = _columns[i] = _columns[j];

                if (actualColumn.name != null)
                    _nameToColumn.Add(actualColumn.name, j);

                for (int y = 0; y < _rowCount; y++)
                    _elements[i, y] = _elements[j, y];
            }

            _columnCount--;
            _columns[_columnCount].Clear();

            for (int y = 0; y < _rowCount; y++)
                _elements[_columnCount, y].value = null;
        }

        public void RemoveRow(int row)
        {
            if (row < 0 || row >= _rowCount) throw new IndexOutOfRangeException();

            for (int i = row, j = row + 1; j < _rowCount; i = j++)
                for (int x = 0; x < _columnCount; x++)
                    _elements[x, i] = _elements[x, j];

            _rowCount--;

            for (int x = 0; x < _columnCount; x++)
            {
                _columns[x].ClearCache();
                _elements[x, _rowCount].value = null;
            }
        }

        #endregion

        #region Clear

        public void ClearRows()
        {
            Empty();
            _rowCount = 0;
        }

        public void ClearColumns()
        {
            Empty();

            for (int x = 0; x < _columnCount; x++)
                _columns[x].Clear();

            _columnCount = 0;
        }

        public void Clear()
        {
            Empty();
            _columnCount = _rowCount = 0;
        }

        #endregion

        #region Empty

        public void EmptyColumn(int column)
        {
            if (column < 0 || column >= _columnCount) throw new IndexOutOfRangeException();

            _columns[column].ClearCache();

            for (int y = 0; y < _rowCount; y++)
                _elements[column, y].value = null;
        }

        public void EmptyRow(int row)
        {
            if (row < 0 || row >= _rowCount) throw new IndexOutOfRangeException();

            for (int x = 0; x < _columnCount; x++)
            {
                _elements[x, row].value = null;
                _columns[x].ClearCache();
            }
        }

        public void Empty()
        {
            for (int x = 0; x < _columnCount; x++)
            {
                _columns[x].ClearCache();

                for (int y = 0; y < _rowCount; y++)
                    _elements[x, y].value = null;
            }
        }

        public void Empty(int column, int row)
        {
            if (column < 0 || row < 0 || column >= _columnCount || row >= _rowCount)
                throw new IndexOutOfRangeException();

            _elements[column, row].value = null;
            _columns[column].ClearCache();
        }

        #endregion

        #region Swap

        public bool SwapColumn(string columnA, int columnB)
        {
            if (TryGetColumnIndex(columnA, out int indexA))
                SwapColumn(indexA, columnB);

            return false;
        }

        public bool SwapColumn(int columnA, string columnB)
        {
            if (TryGetColumnIndex(columnB, out int indexB))
                SwapColumn(columnA, indexB);

            return false;
        }

        public bool SwapColumn(string columnA, string columnB)
        {
            if (TryGetColumnIndex(columnA, out int indexA) && TryGetColumnIndex(columnB, out int indexB))
                SwapColumn(indexA, indexB);

            return false;
        }

        public void SwapColumn(int columnA, int columnB)
        {
            if (columnA < 0 || columnA >= _rowCount || columnB < 0 || columnB >= _rowCount)
                throw new IndexOutOfRangeException();

            Column column = _columns[columnA];
            _columns[columnA] = _columns[columnB];
            _columns[columnB] = column;

            for (int y = 0; y < _rowCount; y++)
            {
                Element temp = _elements[columnA, y];
                _elements[columnA, y] = _elements[columnB, y];
                _elements[columnB, y] = temp;
            }
        }

        public void SwapRow(int rowA, int rowB)
        {
            if (rowA < 0 || rowA >= _rowCount || rowB < 0 || rowB >= _rowCount)
                throw new IndexOutOfRangeException();

            for (int x = 0; x < _columnCount; x++)
            {
                Element temp = _elements[x, rowA];
                _elements[x, rowA] = _elements[x, rowB];
                _elements[x, rowB] = temp;

                _columns[x].ClearCache();
            }
        }

        public void Swap(int columnA, int rowA, int columnB, int rowB)
        {
            if (columnA < 0 || rowA < 0 || columnA >= _columnCount || rowA >= _rowCount
                || columnB < 0 || rowB < 0 || columnB >= _columnCount || rowB >= _rowCount)
                throw new IndexOutOfRangeException();

            Element temp = _elements[columnA, rowA];
            _elements[columnA, rowA] = _elements[columnB, rowB];
            _elements[columnB, rowB] = temp;

            _columns[columnA].ClearCache();
            _columns[columnB].ClearCache();
        }

        #endregion

        #region Move

        public bool MoveColumn(string columnFrom, int columnTo)
        {
            if (TryGetColumnIndex(columnFrom, out int indexA))
                SwapColumn(indexA, columnTo);

            return false;
        }

        public void MoveColumn(int columnFrom, int columnTo)
        {
            if (columnFrom < 0 || columnFrom >= _columnCount || columnTo < 0 || columnTo >= _columnCount)
                throw new IndexOutOfRangeException();

            if (columnFrom != columnTo)
            {
                InsertColumn(columnTo);

                _columns[columnTo] = _columns[columnFrom];
                for (int y = 0; y < _rowCount; y++)
                    _elements[columnTo, y] = _elements[columnFrom, y];

                if (columnFrom < columnTo)
                    RemoveColumn(columnFrom + 1);
                else
                    RemoveColumn(columnFrom);
            }
        }

        public void MoveRow(int rowFrom, int rowTo)
        {
            if (rowFrom < 0 || rowFrom >= _rowCount || rowTo < 0 || rowTo >= _rowCount)
                throw new IndexOutOfRangeException();

            if (rowFrom != rowTo)
            {
                InsertRow(rowTo);

                for (int x = 0; x < _columnCount; x++)
                {
                    _elements[x, rowTo] = _elements[x, rowFrom];
                    _columns[x].ClearCache();
                }

                if (rowFrom < rowTo)
                    RemoveRow(rowFrom + 1);
                else
                    RemoveRow(rowFrom);
            }
        }

        #endregion

        #region Get Column

        public IEnumerable<object> GetColumn(string row) => GetColumn<object>(row);

        public IEnumerable<T> GetColumn<T>(string column)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return GetColumn<T>(columnIndex);

            return null;
        }

        public IEnumerable<object> GetColumn(int row) => GetColumn<object>(row);

        public IEnumerable<T> GetColumn<T>(int column)
        {
            if (column < 0 || column >= _columnCount) throw new IndexOutOfRangeException();

            for (int y = 0; y < _rowCount; y++)
                yield return Get<T>(column, y);
        }

        #endregion

        #region Get Row

        public IEnumerable<object> GetRow(int row) => GetRow<object>(row);

        public IEnumerable<T> GetRow<T>(int row)
        {
            if (row < 0 || row >= _rowCount) throw new IndexOutOfRangeException();

            for (int x = 0; x < _columnCount; x++)
                yield return Get<T>(x, row);
        }

        #endregion

        #region Get

        public object Get(string column, int row) => Get<object>(column, row);

        public T Get<T>(string column, int row)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return Get<T>(columnIndex, row);

            return default(T);
        }

        public object Get(int column, int row) => Get<object>(column, row);

        public T Get<T>(int column, int row)
        {
            TryGet(column, row, out T value);
            return value;
        }

        public bool TryGet<T>(string column, int row, out T value)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return TryGet(columnIndex, row, out value);

            value = default(T);
            return false;
        }

        public bool TryGet<T>(int column, int row, out T value)
        {
            if (column < 0 || row < 0 || column >= _columnCount || row >= _rowCount)
                throw new IndexOutOfRangeException();

            if (_elements[column, row].TryGet(out value))
                return true;

            value = default(T);
            return false;
        }

        #endregion

        #region Set Column

        public bool SetColumn<T>(string column, IEnumerable<T> collection)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
            {
                SetColumn(columnIndex, collection);
                return true;
            }

            return false;
        }

        public void SetColumn<T>(int column, IEnumerable<T> collection)
        {
            if (column < 0 || column >= _columnCount) throw new IndexOutOfRangeException();

            int x = 0;

            foreach (T value in collection)
            {
                if (x >= _rowCount) return;

                Set(column, x++, value);
            }
        }

        #endregion

        #region Set Row

        public bool SetRow<T>(string column, int row, IEnumerable<T> collection)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
            {
                SetColumn(columnIndex, collection);
                return true;
            }

            return false;
        }

        public void SetRow<T>(int column, IEnumerable<T> collection)
        {
            if (column < 0 || column >= _columnCount) throw new IndexOutOfRangeException();

            int x = 0;

            foreach (T value in collection)
            {
                if (x >= _rowCount) return;

                Set(column, x++, value);
            }
        }

        #endregion

        #region Set

        public bool Set(string column, int row, object value)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
            {
                Set(columnIndex, row, value);
                return true;
            }

            return false;
        }

        public void Set(int column, int row, object value)
        {
            if (column < 0 || row < 0 || column >= _columnCount || row >= _rowCount)
                throw new IndexOutOfRangeException();

            _elements[column, row].value = value;
            _columns[column].ClearCache();
        }

        #endregion

        #region To Array

        public object[,] ToArray() => ToArray<object>();

        public T[,] ToArray<T>()
        {
            T[,] array = new T[_columnCount, _rowCount];

            for (int x = 0; x < _columnCount; x++)
                for (int y = 0; y < _rowCount; y++)
                    array[x, y] = Get<T>(x, y);

            return array;
        }

        public void CopyToArray<T>(T[,] array) => CopyToArray(array, 0, 0);

        public void CopyToArray<T>(T[,] array, int arrayIndexX, int arrayIndexY)
            => CopyToArray(0, 0, array, arrayIndexX, arrayIndexY,
            array != null ? Mathf.Max(_columnCount, array.GetLength(0) - arrayIndexX) : 0,
            array != null ? Mathf.Max(_columnCount, array.GetLength(0) - arrayIndexY) : 0);

        public void CopyToArray<T>(int columnIndex, int rowIndex, T[,] array, int arrayIndexX, int arrayIndexY, int countX, int countY)
        {
            if (array == null) throw new NullReferenceException();

            if (columnIndex < 0 || columnIndex >= _columnCount || rowIndex < 0 || rowIndex >= _rowCount
                || arrayIndexX < 0 || arrayIndexX >= _columnCount || arrayIndexY < 0 || arrayIndexY >= _rowCount
                || countX < 0 || arrayIndexX + countX >= _columnCount || columnIndex + countX >= _columnCount
                || countY < 0 || arrayIndexY + countY >= _rowCount || rowIndex + countY >= _rowCount)
                throw new IndexOutOfRangeException();

            for (int x = 0; x < countX; x++)
                for (int y = 0; y < countY; y++)
                    array[arrayIndexX + x, arrayIndexY + y] = Get<T>(columnIndex + x, rowIndex + y);
        }

        #endregion

        #region Column To Array

        public object[] ColumnToArray(string column)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return ColumnToArray(columnIndex);
            
            return null;
        }

        public object[] ColumnToArray(int column) => ColumnToArray<object>(column);


        public T[] ColumnToArray<T>(string column)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return ColumnToArray<T>(columnIndex);

            return null;
        }

        public T[] ColumnToArray<T>(int column)
        {
            if (column < 0 || column >= _columnCount) throw new IndexOutOfRangeException();

            T[] array = new T[_rowCount];

            for (int y = 0; y < _rowCount; y++)
                array[y] = Get<T>(column, y);

            return array;
        }

        public void CopyColumnToArray<T>(string column, T[] array)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                CopyColumnToArray(columnIndex, array);
        }

        public void CopyColumnToArray<T>(int column, T[] array) => CopyColumnToArray(column, array, 0);

        public void CopyColumnToArray<T>(string column, T[] array, int arrayIndex)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                CopyColumnToArray(columnIndex, array, arrayIndex);
        }

        public void CopyColumnToArray<T>(int column, T[] array, int arrayIndex)
            => CopyColumnToArray(column, 0, array, arrayIndex,
            array != null ? Mathf.Max(_columnCount, array.Length - arrayIndex) : 0);

        public void CopyColumnToArray<T>(string column, int rowIndex, T[] array, int arrayIndex, int count)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                CopyColumnToArray(columnIndex, rowIndex, array, arrayIndex, count);
        }

        public void CopyColumnToArray<T>(int column, int rowIndex, T[] array, int arrayIndex, int count)
        {
            if (array == null) throw new NullReferenceException();

            if (rowIndex < 0 || rowIndex >= _columnCount || arrayIndex < 0 || arrayIndex >= _columnCount
                || count < 0 || arrayIndex + count >= _columnCount || rowIndex + count >= _columnCount)
                throw new IndexOutOfRangeException();

            for (int y = 0; y < count; y++)
                array[arrayIndex + y] = Get<T>(column, rowIndex + y);
        }

        #endregion

        #region Row To Array

        public object[] RowToArray(int row) => RowToArray<object>(row);

        public T[] RowToArray<T>(int row)
        {
            if (row < 0 || row >= _rowCount) throw new IndexOutOfRangeException();

            T[] array = new T[_columnCount];

            for (int x = 0; x < _columnCount; x++)
                array[x] = Get<T>(x, row);

            return array;
        }

        public void CopyRowToArray<T>(int row, T[] array) => CopyRowToArray(row, array, 0);

        public void CopyRowToArray<T>(int row, T[] array, int arrayIndex)
            => CopyRowToArray(row, 0, array, arrayIndex,
            array != null ? Mathf.Max(_columnCount, array.Length - arrayIndex) : 0);

        public void CopyRowToArray<T>(int row, int columnIndex, T[] array, int arrayIndex, int count)
        {
            if (array == null) throw new NullReferenceException();

            if (columnIndex < 0 || columnIndex >= _columnCount || arrayIndex < 0 || arrayIndex >= _columnCount
                || count < 0 || arrayIndex + count >= _columnCount || columnIndex + count >= _columnCount)
                throw new IndexOutOfRangeException();

            for (int x = 0; x < count; x++)
                array[arrayIndex + x] = Get<T>(columnIndex + x, row);
        }

        #endregion

        #region Find First Row

        public int FindFirstRow<T>(string column, T value)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return FindFirstRow(columnIndex, value);

            return -1;
        }

        public int FindFirstRow<T>(int column, T value)
        {
            TryFindFirstRow(column, value, out int index);

            return index;
        }

        public bool TryFindFirstRow<T>(string column, T value, out int index)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return TryFindFirstRow(columnIndex, value, out index);

            index = -1;
            return false;
        }

        public bool TryFindFirstRow<T>(int column, T value, out int index)
        {
            if (column < 0 || column >= _columnCount) throw new IndexOutOfRangeException();

            Dictionary<object, Cache> rowByValue = _columns[column].rowByValue;
            if (rowByValue == null) rowByValue = _columns[column].rowByValue = new Dictionary<object, Cache>();

            if (rowByValue.TryGetValue(value, out Cache cache))
            {
                if (cache.first != null)
                {
                    index = cache.first.Value;
                    return index != -1;
                }
            }

            for (int y = 0; y < _columnCount; y++)
            {
                Element node = _elements[column, y];

                if (node.TryGet(out T other) && EqualityComparer<T>.Default.Equals(value, other))
                {
                    rowByValue.Add(value, new Cache(index = y, cache.last));
                    return true;
                }
            }

            index = -1;
            return false;
        }

        #endregion

        #region Find Last Row

        public int FindLastRow<T>(string column, T value)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return FindLastRow(columnIndex, value);

            return -1;
        }

        public int FindLastRow<T>(int column, T value)
        {
            TryFindLastRow(column, value, out int index);

            return index;
        }

        public bool TryFindLastRow<T>(string column, T value, out int index)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return TryFindLastRow(columnIndex, value, out index);

            index = -1;
            return false;
        }

        public bool TryFindLastRow<T>(int column, T value, out int index)
        {
            if (column < 0 || column >= _columnCount) throw new IndexOutOfRangeException();

            Dictionary<object, Cache> rowByValue = _columns[column].rowByValue;
            if (rowByValue == null) rowByValue = _columns[column].rowByValue = new Dictionary<object, Cache>();

            if (rowByValue.TryGetValue(value, out Cache cache))
            {
                if (cache.last != null)
                {
                    index = cache.first.Value;
                    return index != -1;
                }
            }

            for (int y = _columnCount - 1; y >= 0; y--)
            {
                Element node = _elements[column, y];

                if (node.TryGet(out T other) && EqualityComparer<T>.Default.Equals(value, other))
                {
                    rowByValue.Add(value, new Cache(cache.last, index = y));
                    return true;
                }
            }

            index = -1;
            return false;
        }

        #endregion

        #region Find Rows

        public IReadOnlyList<int> FindRows<T>(string column, T value)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return FindRows(columnIndex, value);

            return new HeapAllocationFreeReadOnlyList<int>();
        }

        public IReadOnlyList<int> FindRows<T>(int column, T value)
        {
            if (TryFindRows(column, value, out IReadOnlyList<int> indices))
                return indices;

            return new HeapAllocationFreeReadOnlyList<int>();
        }

        public bool TryFindIndices<T>(string column, T value, out IReadOnlyList<int> indices)
        {
            if (TryGetColumnIndex(column, out int columnIndex))
                return TryFindRows(columnIndex, value, out indices);

            indices = new HeapAllocationFreeReadOnlyList<int>();
            return false;
        }

        public bool TryFindRows<T>(int column, T value, out IReadOnlyList<int> indices)
        {
            if (column < 0 || column >= _columnCount) throw new IndexOutOfRangeException();

            Dictionary<object, Cache> rowByValue = _columns[column].rowByValue;
            if (rowByValue == null) rowByValue = _columns[column].rowByValue = new Dictionary<object, Cache>();

            int first = 0, last = _columnCount;

            if (rowByValue.TryGetValue(value, out Cache cache))
            {
                if (cache.all != null)
                {
                    indices = cache.all;
                    return true;
                }

                if (cache.first != null)
                    first = cache.first.Value;
                else if (cache.last != null)
                    last = cache.last.Value + 1;
            }

            for (int y = first; y < last; y++)
            {
                Element node = _elements[column, y];

                if (node.TryGet(out T other) && EqualityComparer<T>.Default.Equals(value, other))
                    found.Add(y);
            }

            int[] all = found.ToArray();
            rowByValue.Add(value, new Cache(all));
            found.Clear();

            indices = all;
            return false;
        }

        #endregion

        #region Find Row 1

        public int FindRow<T>(string column, T value) => FindFirstRow(column, value);

        public int FindRow<T>(int column, T value) => FindFirstRow(column, value);

        public bool TryFindRow<T>(string column, T value, out int index) => TryFindFirstRow(column, value, out index);

        public bool TryFindRow<T>(int column, T value, out int index) => TryFindFirstRow(column, value, out index);

        #endregion

        #region Find Row 2

        public int FindRow<T1, T2>(string column1, T1 value1, string column2, T2 value2)
        {
            if (TryGetColumnIndex(column1, out int columnIndex1) && TryGetColumnIndex(column2, out int columnIndex2))
                return FindRow(columnIndex1, value1, columnIndex2, value2);

            return -1;
        }

        public int FindRow<T1, T2>(int column1, T1 value1, int column2, T2 value2)
        {
            TryFindRow(column1, value1, column2, value2, out int index);

            return index;
        }

        public bool TryFindRow<T1, T2>(string column1, T1 value1, string column2, T2 value2, out int index)
        {
            if (TryGetColumnIndex(column1, out int columnIndex1) && TryGetColumnIndex(column2, out int columnIndex2))
                return TryFindRow(columnIndex1, value1, columnIndex2, value2, out index);

            index = -1;
            return false;
        }

        public bool TryFindRow<T1, T2>(int column1, T1 value1, int column2, T2 value2, out int index)
        {
            if (column1 < 0 || column1 >= _columnCount || column2 < 0 || column2 >= _columnCount) throw new IndexOutOfRangeException();

            IReadOnlyList<int> rows1 = FindRows(column1, value1), rows2 = FindRows(column2, value2);

            if (rows1.Count > 0 && rows2.Count > 0)
            {
                int index2 = 0, row2 = rows2[index2];

                foreach (int row1 in rows1)
                {
                    while (row2 < row1 && index2 < rows2.Count)
                    {
                        if (++index2 < rows2.Count)
                            row2 = rows2[index2];
                        else
                        {
                            index = -1;
                            return false;
                        }
                    }

                    if (row1 == row2)
                    {
                        index = row1;
                        return true;
                    }
                }
            }

            index = -1;
            return false;
        }

        #endregion

        #region Find Row 3

        public int FindRow<T1, T2, T3>(string column1, T1 value1, string column2, T2 value2, string column3, T3 value3)
        {
            if (TryGetColumnIndex(column1, out int columnIndex1) && TryGetColumnIndex(column2, out int columnIndex2)
                 && TryGetColumnIndex(column3, out int columnIndex3))
                return FindRow(columnIndex1, value1, columnIndex2, value2, columnIndex3, value3);

            return -1;
        }

        public int FindRow<T1, T2, T3>(int column1, T1 value1, int column2, T2 value2, int column3, T3 value3)
        {
            TryFindRow(column1, value1, column2, value2, column3, value3, out int index);

            return index;
        }

        public bool TryFindRow<T1, T2, T3>(string column1, T1 value1, string column2, T2 value2, string column3, T3 value3, out int index)
        {
            if (TryGetColumnIndex(column1, out int columnIndex1) && TryGetColumnIndex(column2, out int columnIndex2)
                 && TryGetColumnIndex(column2, out int columnIndex3))
                return TryFindRow(columnIndex1, value1, columnIndex2, value2, columnIndex3, value3, out index);

            index = -1;
            return false;
        }

        public bool TryFindRow<T1, T2, T3>(int column1, T1 value1, int column2, T2 value2, int column3, T3 value3, out int index)
        {
            if (column1 < 0 || column1 >= _columnCount || column2 < 0 || column2 >= _columnCount
                || column3 < 0 || column3 >= _columnCount) throw new IndexOutOfRangeException();

            IReadOnlyList<int> rows1 = FindRows(column1, value1), rows2 = FindRows(column2, value2),
                rows3 = FindRows(column3, value3);

            if (rows1.Count > 0 && rows2.Count > 0 && rows3.Count > 0)
            {
                int index2 = 0, row2 = rows2[index2],
                    index3 = 0, row3 = rows3[index3];

                foreach (int row1 in rows1)
                {
                    while (row2 < row1 && index2 < rows2.Count)
                    {
                        if (++index2 < rows2.Count)
                            row2 = rows2[index2];
                        else
                        {
                            index = -1;
                            return false;
                        }
                    }

                    if (row1 == row2)
                    {
                        while (row3 < row1 && index3 < rows3.Count)
                        {
                            if (++index3 < rows3.Count)
                                row3 = rows3[index3];
                            else
                            {
                                index = -1;
                                return false;
                            }
                        }

                        if (row1 == row3)
                        {
                            index = row1;
                            return true;
                        }
                    }
                }
            }

            index = -1;
            return false;
        }


        #endregion

        #region Find  Row 4

        public int FindRow<T1, T2, T3, T4>(string column1, T1 value1, string column2, T2 value2, string column3, T3 value3, string column4, T4 value4)
        {
            if (TryGetColumnIndex(column1, out int columnIndex1) && TryGetColumnIndex(column2, out int columnIndex2)
                 && TryGetColumnIndex(column3, out int columnIndex3) && TryGetColumnIndex(column4, out int columnIndex4))
                return FindRow(columnIndex1, value1, columnIndex2, value2, columnIndex3, value3, columnIndex4, value4);

            return -1;
        }

        public int FindRow<T1, T2, T3, T4>(int column1, T1 value1, int column2, T2 value2, int column3, T3 value3, int column4, T4 value4)
        {
            TryFindRow(column1, value1, column2, value2, column3, value3, column4, value4, out int index);

            return index;
        }

        public bool TryFindRow<T1, T2, T3, T4>(string column1, T1 value1, string column2, T2 value2, string column3, T3 value3, string column4, T4 value4, out int index)
        {
            if (TryGetColumnIndex(column1, out int columnIndex1) && TryGetColumnIndex(column2, out int columnIndex2)
                 && TryGetColumnIndex(column2, out int columnIndex3))
                return TryFindRow(columnIndex1, value1, columnIndex2, value2, columnIndex3, value3, out index);

            index = -1;
            return false;
        }

        public bool TryFindRow<T1, T2, T3, T4>(int column1, T1 value1, int column2, T2 value2, int column3, T3 value3, int column4, T4 value4, out int index)
        {
            if (column1 < 0 || column1 >= _columnCount || column2 < 0 || column2 >= _columnCount
                || column3 < 0 || column3 >= _columnCount || column4 < 0 || column4 >= _columnCount) throw new IndexOutOfRangeException();

            IReadOnlyList<int> rows1 = FindRows(column1, value1), rows2 = FindRows(column2, value2),
                rows3 = FindRows(column3, value3), rows4 = FindRows(column4, value4);

            if (rows1.Count > 0 && rows2.Count > 0 && rows3.Count > 0 && rows4.Count > 0)
            {
                int index2 = 0, row2 = rows2[index2],
                    index3 = 0, row3 = rows3[index3],
                    index4 = 0, row4 = rows4[index4];

                foreach (int row1 in rows1)
                {
                    while (row2 < row1 && index2 < rows2.Count)
                    {
                        if (++index2 < rows2.Count)
                            row2 = rows2[index2];
                        else
                        {
                            index = -1;
                            return false;
                        }
                    }

                    if (row1 == row2)
                    {
                        while (row3 < row1 && index3 < rows3.Count)
                        {
                            if (++index3 < rows3.Count)
                                row3 = rows3[index3];
                            else
                            {
                                index = -1;
                                return false;
                            }
                        }

                        if (row1 == row3)
                        {
                            while (row4 < row1 && index4 < rows4.Count)
                            {
                                if (++index4 < rows4.Count)
                                    row4 = rows4[index4];
                                else
                                {
                                    index = -1;
                                    return false;
                                }
                            }

                            if (row1 == row4)
                            {
                                index = row1;
                                return true;
                            }
                        }
                    }
                }
            }

            index = -1;
            return false;
        }

        #endregion

        #region Column Names To String

        public string ColumnNamesToString(string separator = ",")
        {
            string output = "";

            for (int x = 0; x < _columnCount; x++)
            {
                Column column = _columns[x];

                if (column.name != null)
                    output += column.name;

                if (x != _columnCount) output += separator;
            }

            return output;
        }

        public string ColumnNamesToString(string separator, IFormatProvider provider)
        {
            string output = "";

            for (int x = 0; x < _columnCount; x++)
            {
                Column column = _columns[x];

                if (column.name != null)
                    output += column.name.ToString(provider);
                else output += string.Empty.ToString(provider);

                if (x != _columnCount) output += separator;
            }

            return output;
        }
        #endregion

        #region Row To String

        public string RowToString(int row, string separator = ",")
        {
            if (row < 0 || row >= _rowCount) throw new IndexOutOfRangeException();

            string output = "";

            for (int x = 0; x < _columnCount; x++)
            {
                output += _elements[x, row].ValueToString();
                if (x != _columnCount) output += separator;
            }

            return output;
        }

        public string RowToString(int row, string separator, string format)
        {
            if (row < 0 || row >= _rowCount) throw new IndexOutOfRangeException();

            string output = "";

            for (int x = 0; x < _columnCount; x++)
            {
                output += _elements[x, row].ValueToString(format);
                if (x != _columnCount) output += separator;
            }

            return output;
        }

        public string RowToString(int row, string separator, IFormatProvider provider)
        {
            if (row < 0 || row >= _rowCount) throw new IndexOutOfRangeException();

            string output = "";

            for (int x = 0; x < _columnCount; x++)
            {
                output += _elements[x, row].ValueToString(provider);
                if (x != _columnCount) output += separator;
            }

            return output;
        }

        public string RowToString(int row, string separator, string format, IFormatProvider provider)
        {
            if (row < 0 || row >= _rowCount) throw new IndexOutOfRangeException();

            string output = "";

            for (int x = 0; x < _columnCount; x++)
            {
                output += _elements[x, row].ValueToString(format, provider);
                if (x != _columnCount) output += separator;
            }

            return output;
        }

        #endregion

        #region Get To String

        public string GetToString(string column, int row)
            => TryGetColumnIndex(column, out int index) ? GetToString(index, row) : null;

        public string GetToString(int column, int row)
        {
            if (column < 0 || row < 0 || column >= _columnCount || row >= _rowCount)
                throw new IndexOutOfRangeException();

            return _elements[column, row].ValueToString();
        }

        public string GetToString(string column, int row, string format)
            => TryGetColumnIndex(column, out int index) ? GetToString(index, row, format) : null;

        public string GetToString(int column, int row, string format)
        {
            if (column < 0 || row < 0 || column >= _columnCount || row >= _rowCount)
                throw new IndexOutOfRangeException();

            return _elements[column, row].ValueToString(format);
        }

        public string GetToString(string column, int row, IFormatProvider provider)
            => TryGetColumnIndex(column, out int index) ? GetToString(index, row, provider) : null;

        public string GetToString(int column, int row, IFormatProvider provider)
        {
            if (column < 0 || row < 0 || column >= _columnCount || row >= _rowCount)
                throw new IndexOutOfRangeException();

            return _elements[column, row].ValueToString(provider);
        }

        public string GetToString(string column, int row, string format, IFormatProvider provider)
            => TryGetColumnIndex(column, out int index) ? GetToString(index, row, format, provider) : null;

        public string GetToString(int column, int row, string format, IFormatProvider provider)
        {
            if (column < 0 || row < 0 || column >= _columnCount || row >= _rowCount)
                throw new IndexOutOfRangeException();

            return _elements[column, row].ValueToString(format, provider);
        }

        #endregion

        #region To String

        public override string ToString() => ToString(",", "\n");

        public string ToString(string separator = ",", string newLine = "\n")
        {
            string output = ColumnNamesToString(separator);

            for (int y = 0; y < _columnCount; y++)
            {
                output += newLine;
                output += RowToString(y, separator);
            }

            return output;
        }

        public string ToString(string separator, string newLine, string format)
        {
            string output = ColumnNamesToString(separator);

            for (int y = 0; y < _columnCount; y++)
            {
                output += newLine;
                output += RowToString(y, separator, format);
            }

            return output;
        }

        public string ToString(string separator, string newLine, string format, IFormatProvider provider)
        {
            string output = ColumnNamesToString(separator, provider);

            for (int y = 0; y < _columnCount; y++)
            {
                output += newLine;
                output += RowToString(y, separator, format, provider);
            }

            return output;
        }

        public string ToString(string separator, string newLine, IFormatProvider provider)
        {
            string output = ColumnNamesToString(separator, provider);

            for (int y = 0; y < _columnCount; y++)
            {
                output += newLine;
                output += RowToString(y, separator, provider);
            }

            return output;
        }

        #endregion

        private struct Column
        {
            public string name;

            public Dictionary<object, Cache> rowByValue;

            public void Clear()
            {
                name = null;
                ClearCache();
            }

            public void ClearCache() => rowByValue?.Clear();
        }

        private struct Cache
        {
            public readonly int? first;

            public readonly int[] all;

            public readonly int? last;

            public Cache(int[] all)
            {
                this.all = all;

                if (all == null)
                    first = last = null;
                else if (all.Length > 0)
                {
                    first = all[0];
                    last = all[all.Length - 1];
                }
                else
                    first = last = -1;
            }

            public Cache(int? first = null, int? last = null)
            {
                this.first = first;
                this.last = last;

                if (first == last)
                {
                    if (first == null)
                        all = null;
                    else if (first.Value >= 0)
                        all = new int[1] { first.Value };
                    else
                        all = new int[0];
                }
                else
                    all = null;
            }
        }

        public struct Element
        {
            private object _value;
            public object value {
                get => _value;
                set {
                    if (_value != value)
                    {
                        if (value == null)
                        {
                            _type = null;
                            _mode = Mode.Null;
                        }
                        else
                        {
                            Type type = _value.GetType();

                            if (type != _type)
                            {
                                _mode = GetMode(type);
                                _type = type;
                            }
                        }

                        _value = value;
                    }
                }
            }

            private Mode _mode;

            private Type _type;
            public Type valueType;

            private enum Mode
            {
                Null = 0,
                Bool = 1,
                Int = 2,
                Float = 3,
                String = 4,
                Char = 5,
                Other = 6
            }

            private static HashSet<Type> intTypes = new HashSet<Type>()
            {
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long), typeof(ulong)
            };

            private static HashSet<Type> floatTypes = new HashSet<Type>()
            {
                typeof(decimal), typeof(float), typeof(double)
            };

            private static Mode GetMode(Type type)
            {
                if (type == null) return Mode.Null;

                if (type == typeof(bool))
                    return Mode.Bool;

                if (intTypes.Contains(type))
                    return Mode.Int;

                if (floatTypes.Contains(type))
                    return Mode.Float;

                if (type == typeof(char))
                    return Mode.Char;

                if (type == typeof(string))
                    return Mode.Char;

                return Mode.Other;
            }

            public bool TryGet<T>(out T value)
            {
                if (_value is T valueAsT)
                {
                    value = valueAsT;
                    return true;
                }

                if (_mode == Mode.String)
                {
                    string asString = (string)_value;

                    switch (GetMode(typeof(T)))
                    {
                        case Mode.Bool:
                        {
                            if (bool.TryParse((string)_value, out bool asBool) && asBool is T asT)
                            {
                                value = asT;
                                return true;
                            }
                            break;
                        }

                        case Mode.Int:
                        {
                            if (long.TryParse((string)_value, out long asInt) && asInt is T asT)
                            {
                                value = asT;
                                return true;
                            }
                            break;
                        }

                        case Mode.Float:
                        {
                            if (double.TryParse((string)_value, out double asFloat) && asFloat is T asT)
                            {
                                value = asT;
                                return true;
                            }
                            break;
                        }

                        case Mode.Char:
                        {
                            if (asString.Length > 0 && asString[0] is T asT)
                            {
                                value = asT;
                                return true;
                            }
                            break;
                        }
                    }

                    value = default(T);
                    return false;
                }
                else if (_mode == Mode.Bool)
                {
                    bool asBool = (bool)_value;

                    switch (GetMode(typeof(T)))
                    {
                        case Mode.Int:
                        case Mode.Float:
                        {
                            if ((asBool ? 1 : 0) is T asT)
                            {
                                value = asT;
                                return true;
                            }
                            break;
                        }

                        case Mode.String:
                        {
                            if (asBool.ToString() is T asT)
                            {
                                value = asT;
                                return true;
                            }
                            break;
                        }

                        case Mode.Char:
                        {
                            if ((asBool ? 'T' : 'F') is T asT)
                            {
                                value = asT;
                                return true;
                            }
                            break;
                        }
                    }

                    value = default(T);
                    return false;
                }
                else
                {
                    if (_value is T other)
                    {
                        value = other;
                        return true;
                    }
                }

                value = default(T);
                return false;
            }

            public string ValueToString()
            {
                if (value != null) return value.ToString();
                return string.Empty;
            }

            public string ValueToString(string format)
            {
                switch (_mode)
                {
                    case Mode.Int:
                        return ((long)value).ToString(format);
                    case Mode.Float:
                        return ((double)value).ToString(format);
                }

                return ValueToString();
            }

            public string ValueToString(IFormatProvider provider)
            {
                switch (_mode)
                {
                    case Mode.Bool:
                        return ((bool)value).ToString(provider);
                    case Mode.Int:
                        return ((long)value).ToString(provider);
                    case Mode.Float:
                        return ((double)value).ToString(provider);
                    case Mode.Char:
                        return ((char)value).ToString(provider);
                    case Mode.String:
                        return ((string)value).ToString(provider);
                }

                return ValueToString();
            }

            public string ValueToString(string format, IFormatProvider provider)
            {
                switch (_mode)
                {
                    case Mode.Bool:
                        return ((bool)value).ToString(provider);
                    case Mode.Int:
                        return ((long)value).ToString(format, provider);
                    case Mode.Float:
                        return ((double)value).ToString(format, provider);
                    case Mode.Char:
                        return ((char)value).ToString(provider);
                    case Mode.String:
                        return ((string)value).ToString(provider);
                }

                return ValueToString();
            }
        }
    }
}