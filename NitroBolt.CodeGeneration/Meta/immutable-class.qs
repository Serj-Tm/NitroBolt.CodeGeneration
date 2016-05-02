immutable-class
{
  constructor(constructor-arg-s):
    constructor-assign-s;
  field-s;
  with-method(with-arg-s):
    with-assign-s;
};
field
{
  field-name;
  field-type;
  field-default-expression;
};
constructor-arg
{
  constructor-arg-name;
  constructor-arg-type;
};
with-arg
{
  with-arg-name;
  with-arg-type;
};
constructor-assign
{
  constructor-arg-ref;
  field-ref;
  constructor-assign-expression;
  constructor-default-expression;
};
with-assign
{
  with-arg-ref;
  field-ref;
  with-assign-expression;
};

//alias:field-type:ref{field, type};
//alias:constructor-arg-type:ref{constructor-arg, type};
//alias:with-arg-type:ref{with-arg, type};
//alias:with-type:ref{with-arg, type};
'field-type = field.type';
'constructor-arg-type = constructor-arg.type';
'with-arg-type = with-arg.type';
'with-type = with-arg.type';

